import { dotnet } from './dotnet.js'
import fs from "fs";
import path from "path";
import util from 'util';
import url from 'url';
import http from 'http';
import https from 'https';

import express from 'express';
import bodyParser from 'body-parser';
import cors from 'cors';

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .create();

const reportStorageFolder = "../reports";
const dataSourcesFolder = "../data";
const dataSourceFileNames = ['categories', 'support', 'customers'];
const dataSourceURLs = { 'nwind': 'https://demos.devexpress.com/dashboard/Content/nwind.json' };

setModuleImports('main.mjs', {
    dataSource: {
        getJsonData: (jsonUrl) => new Promise(resolve => {
            const requestFn = url.parse(jsonUrl).protocol === 'https:' ? https.get : http.get;
            requestFn(jsonUrl, res => {
                let data = '';
                res.on('data', chunk => data += chunk);
                res.on('end', _ => resolve(data));
            })
            .on('error', () => resolve(''));
        })
    },
    reportStorage: {
        getData: (url) => util.promisify(fs.readFile)(path.join(reportStorageFolder, url)).then(report => ({ report })),
        setData: (buf, url) => util.promisify(fs.writeFile)(path.join(reportStorageFolder, url), buf),
        setNewData: (buf, url) => util.promisify(fs.writeFile)(path.join(reportStorageFolder, url), buf),
        getUrls: () => util.promisify(fs.readdir)(reportStorageFolder)
            .then(files => files.reduce((acc, val) => { acc[val] = val; return acc; }, { "~files": files.join('|') })),

    },
    dataConnectionStorage: {
        getJsonDataConnections: () => {
            const predefinedJsonDataSources = dataSourceFileNames.reduce((acc, val) => {
                const filePath = path.join(dataSourcesFolder, `${val}.json`);
                acc[val] = `Json=${fs.readFileSync(filePath)}`;
                return acc;
            }, {});

            Object.keys(dataSourceURLs).reduce((acc, name) => {
                acc[name] = `Uri=${dataSourceURLs[name]}`;
                return acc;
            }, predefinedJsonDataSources);

            return { ...predefinedJsonDataSources, "~keys": Object.keys(predefinedJsonDataSources).join('|') };
        }
    }
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

const app = express();
app.use(cors());

app.get('/DXXRDV', async (req, res) => {
    const requestArgs = { ...req.query, keys: Object.keys(req.query).join(',') };
    const response = {};
    await exports.WasmAdapter.ProcessViewerRequestAsync(requestArgs, response);
    res.header('Content-Type', "application/json");
    res.send(response.data);
});

app.use(bodyParser.urlencoded({ extended: false }));
app.use(bodyParser.json());

app.post('/DXXRDV', async (req, res) => {
    const requestArgs = { ...req.body, keys: Object.keys(req.body).join(',') };
    const response = {};
    await exports.WasmAdapter.ProcessViewerRequestAsync(requestArgs, response);

    if (typeof response.data !== 'string') {
        const buffer = Buffer.from(response.data);
        res.set({
            'Content-Type': response.contentType,
            'Content-Disposition': `attachment; filename=${response.fileName}`,
            'Access-Control-Expose-Headers': "Content-Disposition",
            'Content-Length': buffer.length
        });
        res.end(buffer);
    } else {
        res.header('Content-Type', "application/json");
        res.send(response.data);
    }
});

app.post('/DXXRD', async (req, res) => {
    const requestArgs = { ...req.body, keys: Object.keys(req.body).join(',') };
    const response = {};
    await exports.WasmAdapter.ProcessDesignerRequestAsync(requestArgs, response);
    res.header('Content-Type', "application/json");
    res.send(response.data);
});

app.post('/DXXQB', async (req, res) => {
    const requestArgs = { ...req.body, keys: Object.keys(req.body).join(',') };
    const response = {};
    await exports.WasmAdapter.ProcessQueryBuilderRequestAsync(requestArgs, response);
    res.header('Content-Type', "application/json");
    res.send(response.data);
});

app.post('/DXXRD/GetReportDesignerModel', async (req, res) => {
    const data = await exports.WasmAdapter.GetReportDesignerModel();
    res.header('Content-Type', "application/json");
    res.send(data);
});

const port = 2009;
app.listen(port, async () => {
    exports.WasmAdapter.Init();
    console.log(`Server is running on port ${port}`);
});
