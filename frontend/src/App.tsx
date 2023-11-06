import './App.css';
import { useEffect, useRef } from 'react';
import { DxReportDesigner } from 'devexpress-reporting/dx-reportdesigner';

const ReportDesigner = () => {
  const viewerRef = useRef();
  const requestOptions = {
    host: "http://localhost:2009/",
    getDesignerModelAction: 'DXXRD/GetReportDesignerModel',
  };

  useEffect(() => {
    const designer = new DxReportDesigner(viewerRef.current, { requestOptions });
    designer.render(); 
    return () => designer.dispose();
  })

  return (<div ref={viewerRef}></div>);
}

function App() {
  return (<div style={{ width: "100%", height: "1000px" }}>
      <ReportDesigner />
  </div>);
  }

export default App;