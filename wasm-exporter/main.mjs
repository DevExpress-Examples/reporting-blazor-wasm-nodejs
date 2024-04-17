import { dotnet } from './_framework/dotnet.js'
import fs from 'fs';
import http from 'http';
import https from 'https';
import url from 'url'

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .create();

setModuleImports('main.mjs', { getJsonData });

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

const repx = fs.readFileSync('../reports/report1.repx');

const model = {
    reportXml: repx,
    exportOptions: {
        DocumentOptions: {
            Application: "WASM",
            Subject: "wasm integration"
        },
        PdfUACompatibility: "PdfUA1"
    }
}
const result = {};
await exports.ReportExporter.ExportToPdfAsync(model, result);
const buffer = Buffer.from(result.pdf);
fs.writeFileSync('result.pdf', buffer);

function getJsonData(jsonUrl) {
    return new Promise((callback) => {
        const getMethod = url.parse(jsonUrl).protocol === 'https:' ? https.get : http.get;
        getMethod(jsonUrl, res => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', _ => callback(data));
        }).on('error', () => callback(''));
    });
}
await dotnet.run();
