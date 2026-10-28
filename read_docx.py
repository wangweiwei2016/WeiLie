# -*- coding: utf-8 -*-
"""零依赖 docx 解析脚本 - 仅使用 Python 标准库"""
import zipfile
import xml.etree.ElementTree as ET
import sys
import re

# 注册 XML 命名空间
NS = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}

def extract_text_from_docx(docx_path):
    """从 docx 中提取段落和表格文本"""
    results = []
    
    with zipfile.ZipFile(docx_path, 'r') as z:
        # 读取 document.xml
        with z.open('word/document.xml') as f:
            tree = ET.parse(f)
            root = tree.getroot()
        
        # 遍历 body 下的所有直接子元素
        body = root.find('.//w:body', NS)
        if body is None:
            return []
        
        for elem in body:
            tag = elem.tag.split('}')[-1]
            
            if tag == 'p':  # 段落
                texts = []
                style = ''
                pPr = elem.find('w:pPr', NS)
                if pPr is not None:
                    pStyle = pPr.find('w:pStyle', NS)
                    if pStyle is not None:
                        style = pStyle.get('{%s}val' % NS['w'], '')
                
                for t in elem.iter('{%s}t' % NS['w']):
                    if t.text:
                        texts.append(t.text)
                
                line = ''.join(texts).strip()
                if line:
                    results.append(f"[{style}] {line}" if style else line)
            
            elif tag == 'tbl':  # 表格
                results.append("\n=== 表格 ===")
                for tr in elem.findall('w:tr', NS):
                    row_data = []
                    for tc in tr.findall('w:tc', NS):
                        cell_texts = []
                        for t in tc.iter('{%s}t' % NS['w']):
                            if t.text:
                                cell_texts.append(t.text)
                        row_data.append(''.join(cell_texts).strip())
                    results.append(' | '.join(row_data))
                results.append("=== 表格结束 ===\n")
    
    return results

if __name__ == '__main__':
    docx_path = r"e:\喵喵\数码项目\数码兽-围猎.docx"
    
    # 也支持命令行传入路径
    if len(sys.argv) > 1:
        docx_path = sys.argv[1]
    
    print(f"正在解析: {docx_path}\n")
    print("=" * 60)
    
    lines = extract_text_from_docx(docx_path)
    for line in lines:
        print(line)
    
    print("\n" + "=" * 60)
    print(f"共提取 {len(lines)} 行内容")