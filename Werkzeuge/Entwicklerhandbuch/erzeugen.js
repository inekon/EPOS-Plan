// Erzeugt die Word-Fassung des Entwicklerhandbuchs Administration und Support aus der Markdown-Quelle.
// Aufruf aus der Repowurzel: node Werkzeuge/Entwicklerhandbuch/erzeugen.js  (Modul docx, siehe LIESMICH.md)
const path=require('path');
const WURZEL=path.resolve(__dirname,'..','..');
const fs=require('fs');
const D=require('docx');
const {Document,Packer,Paragraph,TextRun,Table,TableRow,TableCell,WidthType,ShadingType,BorderStyle,HeadingLevel,AlignmentType,Header,Footer,PageNumber,TableOfContents,LevelFormat,PageOrientation,SectionType}=D;
const SRC=path.join(WURZEL,'Dokumentation/aktuell/Entwicklerhandbuch_Administration_Support_EPOS-Plan.md');
const OUT=path.join(WURZEL,'Dokumentation/aktuell/Entwicklerhandbuch_Administration_Support_EPOS-Plan.docx');
const lines=fs.readFileSync(SRC,'utf8').split(/\r?\n/);
const BLUE='1F3864';
function inline(t,base={}){
  t=t.replace(/\[([^\]]+)\]\([^)]*\)/g,'$1');
  const runs=[];
  const re=/(`[^`]+`|\*\*[^*]+\*\*|~~[^~]+~~|\*[^*\s][^*]*\*)/g;
  let last=0,m;
  const push=(s,o)=>{if(s)runs.push(new TextRun({text:s,...base,...o}));};
  while((m=re.exec(t))){
    push(t.slice(last,m.index),{});
    const x=m[0];
    if(x.startsWith('`')){ // code, may sit inside bold
      push(x.slice(1,-1),{font:'Consolas',size:base.size?base.size-1:20});
    } else if(x.startsWith('**')){
      const inner=x.slice(2,-2);
      // allow code inside bold
      inner.split(/(`[^`]+`)/).forEach(p=>{ if(p.startsWith('`'))push(p.slice(1,-1),{font:'Consolas',bold:true,size:base.size?base.size-1:20}); else push(p,{bold:true});});
    } else if(x.startsWith('~~')) push(x.slice(2,-2),{strike:true});
    else push(x.slice(1,-1),{italics:true});
    last=m.index+x.length;
  }
  push(t.slice(last),{});
  return runs;
}
const bd={style:BorderStyle.SINGLE,size:4,color:'A6A6A6'};
const borders={top:bd,bottom:bd,left:bd,right:bd};
function table(rows,width){
  const n=rows[0].length;
  // column widths by max content length
  const w=Array(n).fill(0);
  rows.forEach(r=>r.forEach((c,i)=>{w[i]=Math.max(w[i],Math.min(60,Math.max(6,c.replace(/[`*]/g,'').length)));}));
  const tot=w.reduce((a,b)=>a+b,0);
  let cw=w.map(x=>Math.floor(x/tot*width));
  cw[n-1]+=width-cw.reduce((a,b)=>a+b,0);
  return new Table({width:{size:width,type:WidthType.DXA},columnWidths:cw,rows:rows.map((r,ri)=>new TableRow({
    tableHeader:ri===0,cantSplit:true,
    children:r.map((c,i)=>new TableCell({width:{size:cw[i],type:WidthType.DXA},borders,
      shading:ri===0?{type:ShadingType.CLEAR,fill:'D9D9D9',color:'auto'}:undefined,
      margins:{top:50,bottom:50,left:90,right:90},
      children:[new Paragraph({spacing:{after:0},children:inline(c,{size:ri===0?19:19,...(ri===0?{bold:true}:{})})})]}))}))});
}
// parse into blocks
const blocks=[];let i=0;let numId=0;
while(i<lines.length){
  const l=lines[i];
  if(/^# /.test(l)){i++;continue;}
  if(/^### /.test(l)){blocks.push({k:'h',lv:2,t:l.slice(4)});i++;continue;}
  if(/^## /.test(l)){blocks.push({k:'h',lv:1,t:l.slice(3)});i++;continue;}
  if(/^```/.test(l)){const c=[];i++;while(!/^```/.test(lines[i])){c.push(lines[i]);i++;}i++;blocks.push({k:'code',c});continue;}
  if(/^\|/.test(l)){const rows=[];while(i<lines.length&&/^\|/.test(lines[i])){const s=lines[i];if(!/^\|[-: |]*\|$/.test(s)){rows.push(s.replace(/^\||\|$/g,'').split('|').map(x=>x.trim()));}i++;}blocks.push({k:'table',rows});continue;}
  if(/^- /.test(l)){const it=[];while(i<lines.length&&(/^- /.test(lines[i])||/^  \S/.test(lines[i]))){if(/^- /.test(lines[i]))it.push(lines[i].slice(2));else it[it.length-1]+=' '+lines[i].trim();i++;}blocks.push({k:'ul',it});continue;}
  if(/^\d+\. /.test(l)){const it=[];while(i<lines.length&&(/^\d+\. /.test(lines[i])||/^   \S/.test(lines[i]))){if(/^\d+\. /.test(lines[i]))it.push(lines[i].replace(/^\d+\. /,''));else it[it.length-1]+=' '+lines[i].trim();i++;}blocks.push({k:'ol',it,id:++numId});continue;}
  if(l.trim()===''){i++;continue;}
  const p=[];while(i<lines.length&&lines[i].trim()!==''&&!/^(#|```|\||- |\d+\. )/.test(lines[i])){p.push(lines[i].trim());i++;}
  blocks.push({k:'p',t:p.join(' ')});
}
function render(b,width){
  switch(b.k){
    case 'h': return [new Paragraph({heading:b.lv===1?HeadingLevel.HEADING_1:HeadingLevel.HEADING_2,children:[new TextRun(b.t)]})];
    case 'p': return [new Paragraph({spacing:{after:120},children:inline(b.t)})];
    case 'ul': return b.it.map(t=>new Paragraph({numbering:{reference:'bul',level:0},spacing:{after:60},children:inline(t)}));
    case 'ol': return b.it.map(t=>new Paragraph({numbering:{reference:'num'+b.id,level:0},spacing:{after:60},children:inline(t)}));
    case 'code': return b.c.map(t=>new Paragraph({spacing:{after:0},shading:{type:ShadingType.CLEAR,fill:'F2F2F2',color:'auto'},children:[new TextRun({text:t,font:'Consolas',size:18})]})).concat([new Paragraph({spacing:{after:120},children:[]})]);
    case 'table': return [table(b.rows,width),new Paragraph({spacing:{after:120},children:[]})];
  }
}
const PW=9638, LW=15000; // content widths (A4 portrait 2cm margins ; landscape)
const ids=[...new Set(blocks.filter(b=>b.k==='ol').map(b=>b.id))];
const numbering={config:[{reference:'bul',levels:[{level:0,format:LevelFormat.BULLET,text:'•',alignment:AlignmentType.LEFT,style:{paragraph:{indent:{left:540,hanging:270}}}}]},
 ...ids.map(id=>({reference:'num'+id,levels:[{level:0,format:LevelFormat.DECIMAL,text:'%1.',alignment:AlignmentType.LEFT,style:{paragraph:{indent:{left:540,hanging:360}}}}]}))]};
// split sections: landscape from heading 2.1 through its first table
const s21=blocks.findIndex(b=>b.k==='h'&&b.t.startsWith('2.1 '));
const t21=blocks.findIndex((b,j)=>j>s21&&b.k==='table');
const parts=[[0,s21,false],[s21,t21+1,true],[t21+1,blocks.length,false]];
const mk=(land)=>({page:{size:{width:11906,height:16838,orientation:land?PageOrientation.LANDSCAPE:PageOrientation.PORTRAIT},margin:{top:1417,bottom:1417,left:1134,right:1134}}});
const header=()=>new Header({children:[new Paragraph({border:{bottom:{style:BorderStyle.SINGLE,size:4,color:'A6A6A6',space:2}},children:[new TextRun({text:'EPOS-Plan · Entwicklerhandbuch Administration und Support',size:18,color:'595959'})]})]});
const footer=()=>new Footer({children:[new Paragraph({alignment:AlignmentType.CENTER,children:[new TextRun({text:'Seite ',size:18}),new TextRun({children:[PageNumber.CURRENT],size:18}),new TextRun({text:' von ',size:18}),new TextRun({children:[PageNumber.TOTAL_PAGES],size:18})]})]});
const title=[
 new Paragraph({spacing:{before:1200,after:200},children:[new TextRun({text:'EPOS-Plan — Entwicklerhandbuch: Administration und Support',bold:true,size:44,color:BLUE})]}),
 new Paragraph({spacing:{after:480},border:{bottom:{style:BorderStyle.SINGLE,size:8,color:BLUE,space:6}},children:[new TextRun({text:'Stand 11.10.2026 (Stufe 1 umgesetzt, Nachzug Zapfprofil-Vorgaben offen) · INEKON, nur für Entwicklung und Support',size:22,color:'595959'})]}),
 new Paragraph({spacing:{after:120},children:[new TextRun({text:'Inhaltsverzeichnis',bold:true,size:28,color:BLUE})]}),
 new TableOfContents('Inhaltsverzeichnis',{hyperlink:true,headingStyleRange:'1-2'}),
 new Paragraph({children:[new D.PageBreak()]}),
];
const sections=parts.filter(p=>p[1]>p[0]).map((p,idx)=>({
  properties:{...mk(p[2]),type:idx===0?undefined:SectionType.NEXT_PAGE},
  headers:{default:header()},footers:{default:footer()},
  children:[...(idx===0?title:[]),...blocks.slice(p[0],p[1]).flatMap(b=>render(b,p[2]?LW-300:PW))]}));
const H=(id,name,size,before)=>({id,name,basedOn:'Normal',next:'Normal',quickFormat:true,run:{size,bold:true,color:BLUE,font:'Calibri'},paragraph:{spacing:{before,after:120},keepNext:true,outlineLevel:id==='Heading1'?0:1}});
const doc=new Document({features:{updateFields:true},numbering,
 styles:{default:{document:{run:{font:'Calibri',size:22,language:{value:'de-DE'}}}},paragraphStyles:[H('Heading1','Heading 1',32,360),H('Heading2','Heading 2',26,240)]},
 sections});
Packer.toBuffer(doc).then(b=>{fs.writeFileSync(OUT,b);console.log('ok',b.length);});
