#!/usr/bin/env python3
import sys
if '--allow-legacy-overwrite' not in sys.argv:
    raise SystemExit('Historical v4 generator: refuses to overwrite approved v5 assets. See CONTRIBUTING.md.')

from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT_H = ROOT / 'include' / 'PixelArtIndexed4.h'
ASSET_DIR = ROOT / 'assets' / 'exact_indexed4'
ASSET_DIR.mkdir(parents=True, exist_ok=True)

W = H = 82
# Global RGB565 palette; keep <=16 colors so pixels pack to 4 bpp.
PALETTE565 = [
    0x0000, # 0 black
    0x1082, # 1 near black
    0x2104, # 2 dark graphite
    0x4208, # 3 graphite
    0x7BEF, # 4 mid silver
    0xC618, # 5 light silver
    0xFFFF, # 6 white
    0xF800, # 7 AMD red
    0xA000, # 8 AMD dark red
    0x001F, # 9 Intel blue
    0x051F, # 10 Intel bright blue
    0x07FF, # 11 cyan
    0x07E0, # 12 NVIDIA green
    0x03E0, # 13 NVIDIA dark green
    0xFFE0, # 14 yellow
    0xFD20, # 15 orange
]

def rgb565_to_rgb(v):
    return ((v >> 11 & 0x1F) * 255 // 31,
            (v >> 5 & 0x3F) * 255 // 63,
            (v & 0x1F) * 255 // 31)

RGB = [rgb565_to_rgb(v) for v in PALETTE565]

FONT = {
 'A':[0b01110,0b10001,0b10001,0b11111,0b10001,0b10001,0b10001],
 'B':[0b11110,0b10001,0b10001,0b11110,0b10001,0b10001,0b11110],
 'C':[0b01111,0b10000,0b10000,0b10000,0b10000,0b10000,0b01111],
 'D':[0b11110,0b10001,0b10001,0b10001,0b10001,0b10001,0b11110],
 'E':[0b11111,0b10000,0b10000,0b11110,0b10000,0b10000,0b11111],
 'F':[0b11111,0b10000,0b10000,0b11110,0b10000,0b10000,0b10000],
 'G':[0b01111,0b10000,0b10000,0b10111,0b10001,0b10001,0b01111],
 'H':[0b10001,0b10001,0b10001,0b11111,0b10001,0b10001,0b10001],
 'I':[0b11111,0b00100,0b00100,0b00100,0b00100,0b00100,0b11111],
 'J':[0b00111,0b00010,0b00010,0b00010,0b10010,0b10010,0b01100],
 'K':[0b10001,0b10010,0b10100,0b11000,0b10100,0b10010,0b10001],
 'L':[0b10000,0b10000,0b10000,0b10000,0b10000,0b10000,0b11111],
 'M':[0b10001,0b11011,0b10101,0b10101,0b10001,0b10001,0b10001],
 'N':[0b10001,0b11001,0b10101,0b10011,0b10001,0b10001,0b10001],
 'O':[0b01110,0b10001,0b10001,0b10001,0b10001,0b10001,0b01110],
 'P':[0b11110,0b10001,0b10001,0b11110,0b10000,0b10000,0b10000],
 'Q':[0b01110,0b10001,0b10001,0b10001,0b10101,0b10010,0b01101],
 'R':[0b11110,0b10001,0b10001,0b11110,0b10100,0b10010,0b10001],
 'S':[0b01111,0b10000,0b10000,0b01110,0b00001,0b00001,0b11110],
 'T':[0b11111,0b00100,0b00100,0b00100,0b00100,0b00100,0b00100],
 'U':[0b10001,0b10001,0b10001,0b10001,0b10001,0b10001,0b01110],
 'V':[0b10001,0b10001,0b10001,0b10001,0b10001,0b01010,0b00100],
 'W':[0b10001,0b10001,0b10001,0b10101,0b10101,0b10101,0b01010],
 'X':[0b10001,0b10001,0b01010,0b00100,0b01010,0b10001,0b10001],
 'Y':[0b10001,0b10001,0b01010,0b00100,0b00100,0b00100,0b00100],
 'Z':[0b11111,0b00001,0b00010,0b00100,0b01000,0b10000,0b11111],
 '0':[0b01110,0b10001,0b10011,0b10101,0b11001,0b10001,0b01110],
 '1':[0b00100,0b01100,0b00100,0b00100,0b00100,0b00100,0b01110],
 '2':[0b01110,0b10001,0b00001,0b00010,0b00100,0b01000,0b11111],
 '3':[0b11110,0b00001,0b00001,0b01110,0b00001,0b00001,0b11110],
 '4':[0b00010,0b00110,0b01010,0b10010,0b11111,0b00010,0b00010],
 '5':[0b11111,0b10000,0b10000,0b11110,0b00001,0b00001,0b11110],
 '6':[0b01110,0b10000,0b10000,0b11110,0b10001,0b10001,0b01110],
 '7':[0b11111,0b00001,0b00010,0b00100,0b01000,0b01000,0b01000],
 '8':[0b01110,0b10001,0b10001,0b01110,0b10001,0b10001,0b01110],
 '9':[0b01110,0b10001,0b10001,0b01111,0b00001,0b00001,0b01110],
 '-':[0,0,0,0b11111,0,0,0],
 ' ':[0,0,0,0,0,0,0],
}

def text_width(s, scale=1, spacing=1):
    if not s: return 0
    return len(s)*5*scale + (len(s)-1)*spacing*scale

def draw_text(img, x, y, s, color, scale=1, spacing=1):
    for ch in s.upper():
        glyph = FONT.get(ch, FONT[' '])
        for gy,row in enumerate(glyph):
            for gx in range(5):
                if row & (1 << (4-gx)):
                    for sy in range(scale):
                        for sx in range(scale):
                            px=x+gx*scale+sx; py=y+gy*scale+sy
                            if 0 <= px < W and 0 <= py < H:
                                img[py][px]=color
        x += (5+spacing)*scale

def centered_text(img, y, s, color, scale=1, spacing=1):
    x=(W-text_width(s,scale,spacing))//2
    draw_text(img,x,y,s,color,scale,spacing)

def fill_rect(img, x0,y0,x1,y1,c):
    x0=max(0,x0); y0=max(0,y0); x1=min(W-1,x1); y1=min(H-1,y1)
    for y in range(y0,y1+1):
        for x in range(x0,x1+1): img[y][x]=c

def hline(img,x0,x1,y,c,t=1): fill_rect(img,x0,y,x1,y+t-1,c)
def vline(img,x,y0,y1,c,t=1): fill_rect(img,x,y0,x+t-1,y1,c)

def cut_frame(img,x0,y0,x1,y1,c,t=3,cut=5):
    # polygon-like square frame with cut corners, integer/pixel only
    for k in range(t):
        xa=x0+k; ya=y0+k; xb=x1-k; yb=y1-k; cc=max(1,cut-k)
        hline(img,xa+cc,xb-cc,ya,c); hline(img,xa+cc,xb-cc,yb,c)
        vline(img,xa,ya+cc,yb-cc,c); vline(img,xb,ya+cc,yb-cc,c)
        # 45-degree corner pixels
        for d in range(cc):
            if d < cc:
                pts=[(xa+d,ya+cc-d),(xb-d,ya+cc-d),(xa+d,yb-cc+d),(xb-d,yb-cc+d)]
                for x,y in pts:
                    if 0<=x<W and 0<=y<H: img[y][x]=c

def new_img(): return [[0 for _ in range(W)] for _ in range(H)]

def cpu_art(vendor, style):
    img=new_img()
    accent = 7 if vendor=='AMD' else 10
    accent_dark = 8 if vendor=='AMD' else 9
    # background is black; add subtle dark panel blocks for better TFT contrast
    if style == 1:
        cut_frame(img,3,3,78,78,accent,4,6)
        fill_rect(img,12,12,69,69,2)
        cut_frame(img,12,12,69,69,5,2,4)
        fill_rect(img,18,18,63,63,3)
    elif style == 2:
        cut_frame(img,3,3,78,78,5,2,6)
        fill_rect(img,6,9,13,72,accent)
        fill_rect(img,68,9,75,72,accent_dark)
        fill_rect(img,17,12,64,69,2)
        cut_frame(img,17,12,64,69,5,2,4)
        fill_rect(img,22,17,59,64,3)
    elif style == 5:
        cut_frame(img,7,7,74,74,4,2,5)
        # colored contact/pin blocks around the package
        for p in range(12,70,9):
            fill_rect(img,p,2,p+4,6,accent); fill_rect(img,p,75,p+4,79,accent_dark)
            fill_rect(img,2,p,6,p+4,accent); fill_rect(img,75,p,79,p+4,accent_dark)
        fill_rect(img,14,14,67,67,2)
        cut_frame(img,14,14,67,67,5,2,4)
        fill_rect(img,20,20,61,61,3)
    else: # style 7
        # asymmetric, flat diagonal motif
        cut_frame(img,3,3,78,78,accent_dark,2,9)
        fill_rect(img,3,18,8,63,accent); fill_rect(img,73,18,78,63,accent)
        hline(img,18,63,3,accent,5); hline(img,18,63,74,accent_dark,5)
        fill_rect(img,13,13,68,68,2)
        cut_frame(img,13,13,68,68,5,2,6)
        # diagonal corner flags
        for d in range(10):
            fill_rect(img,13+d,13,13+d,22-d,accent)
            fill_rect(img,68-d,59+d,68-d,68,accent_dark)
        fill_rect(img,21,21,60,60,3)
    # central brand mark, deliberately gray/white; brand color stays structural
    if vendor=='AMD':
        centered_text(img,30,'AMD',6,2)
        centered_text(img,51,'CPU',4,1)
    else:
        centered_text(img,30,'INTEL',6,1)
        centered_text(img,48,'CORE',4,1)
    # tiny corner index marks, flat and deterministic
    fill_rect(img,10,10,12,12,6); fill_rect(img,69,69,71,71,6)
    return img

def fan_icon(img,cx,cy,c,accent):
    # Flat octagonal fan: no gradients, only crisp pixels.
    # Outer ring
    for dx,dy in [(-7,-9),(-3,-10),(1,-10),(5,-9),(8,-6),(9,-2),(9,2),(7,6),
                  (3,9),(-1,9),(-5,8),(-8,5),(-9,1),(-9,-3),(-7,-7)]:
        fill_rect(img,cx+dx,cy+dy,cx+dx+2,cy+dy+2,c)
    fill_rect(img,cx-2,cy-2,cx+2,cy+2,6)
    # four accent blades
    fill_rect(img,cx-1,cy-7,cx+2,cy-3,accent)
    fill_rect(img,cx+3,cy-1,cx+7,cy+2,accent)
    fill_rect(img,cx-2,cy+3,cx+1,cy+7,accent)
    fill_rect(img,cx-7,cy-2,cx-3,cy+1,accent)

def draw_model_block(img, model, accent):
    # Discrete GPU model area, intentionally graphical rather than text-only.
    if model == 'GTX':
        draw_text(img,39,31,'GTX',6,1)
    elif model == 'GTXTI':
        draw_text(img,39,29,'GTX',6,1)
        fill_rect(img,39,49,60,57,accent); draw_text(img,43,50,'TI',0,1)
    elif model == 'RTX':
        draw_text(img,39,31,'RTX',6,1)
    elif model == 'RTXTI':
        draw_text(img,39,29,'RTX',6,1)
        fill_rect(img,39,49,60,57,accent); draw_text(img,43,50,'TI',0,1)
    elif model == 'SUPER':
        draw_text(img,39,27,'RTX',6,1)
        fill_rect(img,36,47,69,57,accent); draw_text(img,38,49,'SUPER',0,1)
    elif model == 'PRO':
        draw_text(img,41,29,'PRO',6,1)
        hline(img,39,65,51,accent,3)
    elif model == 'RADEON':
        draw_text(img,35,31,'RADEON',6,1)
    elif model == 'RDNA':
        draw_text(img,39,30,'RDNA',6,1)
        hline(img,39,64,50,accent,2)
    elif model == 'ARC':
        draw_text(img,42,29,'ARC',6,1)
        # Intel Arc diagonal signature, still flat pixel blocks
        for d in range(0,16,3):
            fill_rect(img,39+d,49-d//2,42+d,51-d//2,accent)
    else:
        draw_text(img,39,31,model,6,1)

def gpu_art(vendor, model, igpu=False):
    img=new_img()
    if vendor=='NVIDIA': accent=12; dark=13; brand='NVIDIA'
    elif vendor=='AMD': accent=7; dark=8; brand='AMD'
    else: accent=10; dark=9; brand='INTEL'

    if igpu:
        # Flat chip tile, not a discrete card silhouette.
        cut_frame(img,7,7,74,74,accent,3,6)
        for p in range(14,69,10):
            fill_rect(img,p,2,p+3,6,dark); fill_rect(img,p,75,p+3,79,accent)
            fill_rect(img,2,p,6,p+3,dark); fill_rect(img,75,p,79,p+3,accent)
        fill_rect(img,14,14,67,67,2)
        cut_frame(img,14,14,67,67,5,1,4)
        centered_text(img,19,brand,4,1)
        sc=2 if len(model)<=4 else 1
        centered_text(img,36,model,6,sc)
        centered_text(img,58,'IGPU',accent,1)
        return img

    # Discrete GPU: flat card silhouette. Left fan + right model panel.
    centered_text(img,4,brand,accent,1)
    fill_rect(img,3,17,78,66,1)
    cut_frame(img,3,17,78,66,accent,3,5)
    fill_rect(img,9,23,72,60,2)
    # accent connector / card edge
    fill_rect(img,12,62,30,65,14 if vendor!='INTEL' else 10)
    fan_icon(img,23,42,4,accent)
    vline(img,34,27,57,3,1)
    draw_model_block(img,model,accent)
    return img

def pack4(img):
    out=[]
    for y in range(H):
        for x in range(0,W,2):
            a=img[y][x] & 0xF
            b=img[y][x+1] & 0xF
            out.append((a<<4)|b)
    assert len(out)==(W//2)*H
    return out

def save_png(name,img,scale=1):
    im=Image.new('RGB',(W,H),RGB[0]); p=im.load()
    for y in range(H):
        for x in range(W): p[x,y]=RGB[img[y][x]]
    if scale!=1: im=im.resize((W*scale,H*scale),Image.Resampling.NEAREST)
    im.save(ASSET_DIR/f'{name}.png')
    return im

assets=[]
for vendor in ('AMD','INTEL'):
    for style in (1,2,5,7):
        assets.append((f'CPU_{vendor}_STYLE{style}_I4', cpu_art(vendor,style)))

for name,vendor,model,igpu in [
    ('GPU_NVIDIA_GTX_I4','NVIDIA','GTX',False),
    ('GPU_NVIDIA_GTX_TI_I4','NVIDIA','GTXTI',False),
    ('GPU_NVIDIA_RTX_I4','NVIDIA','RTX',False),
    ('GPU_NVIDIA_RTX_TI_I4','NVIDIA','RTXTI',False),
    ('GPU_NVIDIA_RTX_SUPER_I4','NVIDIA','SUPER',False),
    ('GPU_NVIDIA_PRO_I4','NVIDIA','PRO',False),
    ('GPU_AMD_RADEON_I4','AMD','RADEON',False),
    ('GPU_AMD_RDNA_I4','AMD','RDNA',False),
    ('GPU_INTEL_ARC_I4','INTEL','ARC',False),
    ('GPU_INTEL_UHD_IGPU_I4','INTEL','UHD',True),
    ('GPU_INTEL_XE_IGPU_I4','INTEL','XE',True),
    ('GPU_AMD_RADEON_IGPU_I4','AMD','RADEON',True),
    ('GPU_AMD_RDNA_IGPU_I4','AMD','RDNA',True),
]: assets.append((name,gpu_art(vendor,model,igpu)))

# Save exact individual source PNGs and contact sheet generated from the same arrays.
for name,img in assets: save_png(name,img)

cols=5; cell=190; title=32
rows=(len(assets)+cols-1)//cols
sheet=Image.new('RGB',(cols*cell,rows*(cell+title)),(18,18,18)); d=ImageDraw.Draw(sheet)
for i,(name,img) in enumerate(assets):
    cx=(i%cols)*cell; cy=(i//cols)*(cell+title)
    tile=save_png(name,img,2)
    sheet.paste(tile,(cx+13,cy+title))
    d.text((cx+6,cy+8),name.replace('_I4',''),fill=(240,240,240))
sheet.save(ROOT/'assets'/'preview_exact_indexed4.png')

# Emit C++ header from exact data.
lines=[]
lines += ['#pragma once','#include <Arduino.h>','',
          '// Auto-generated exact 82x82 4-bit indexed pixel art.',
          '// Two pixels per byte, high nibble first. Renderer expands to RGB565.',
          'static constexpr uint8_t PHAT_ART_W = 82;',
          'static constexpr uint8_t PHAT_ART_H = 82;',
          'static constexpr uint8_t PHAT_ART_ROW_BYTES = 41;',
          'static constexpr size_t PHAT_ART_PACKED_BYTES = 3362;','',
          'const uint16_t PHAT_ART_PALETTE[16] PROGMEM = {']
for i,v in enumerate(PALETTE565):
    lines.append(f'  0x{v:04X},{" // " + str(i) if i<16 else ""}')
lines += ['};','']
for name,img in assets:
    data=pack4(img)
    lines.append(f'const uint8_t {name}[PHAT_ART_PACKED_BYTES] PROGMEM = {{')
    for i in range(0,len(data),16):
        lines.append('  '+', '.join(f'0x{x:02X}' for x in data[i:i+16])+',')
    lines.append('};')
    lines.append('')
OUT_H.write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(f'wrote {OUT_H}')
print(f'assets={len(assets)} packed={len(assets)*3362} bytes')
