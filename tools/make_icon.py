import math
from PIL import Image, ImageDraw

S = 1024
SS = 4  # supersample
W = S * SS

CREAM_TOP = (253, 249, 236)
CREAM_BOT = (239, 228, 198)
ORANGE = (210, 84, 30)
AMBER = (232, 138, 42)

def cubic(p0, p1, p2, p3, n=160):
    pts = []
    for i in range(n + 1):
        t = i / n
        mt = 1 - t
        x = (mt**3)*p0[0] + 3*(mt**2)*t*p1[0] + 3*mt*(t**2)*p2[0] + (t**3)*p3[0]
        y = (mt**3)*p0[1] + 3*(mt**2)*t*p1[1] + 3*mt*(t**2)*p2[1] + (t**3)*p3[1]
        pts.append((x, y))
    return pts

def flame_points(cx, cy, scale, tip=(0.56, 0.05)):
    """归一化火焰轮廓，再按 (cx, cy, scale) 缩放"""
    pts = []
    segs = [
        ((0.50, 0.98), (0.20, 0.94), (0.09, 0.70), (0.19, 0.50)),
        ((0.19, 0.50), (0.28, 0.36), (0.40, 0.26), (0.42, 0.14)),
        ((0.42, 0.14), (0.47, 0.20), tip, (tip[0] + 0.10, tip[1] + 0.03)),
        ((tip[0] + 0.10, tip[1] + 0.03), (0.78, 0.20), (0.86, 0.34), (0.85, 0.52)),
        ((0.85, 0.52), (0.84, 0.74), (0.80, 0.90), (0.50, 0.98)),
    ]
    for p0, p1, p2, p3 in segs:
        pts += cubic(p0, p1, p2, p3, 120)
    return [(cx + (x - 0.5) * scale, cy + (y - 0.5) * scale) for x, y in pts]

def build():
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    # 背景：圆角方块 + 竖向渐变
    grad = Image.new("RGBA", (W, W))
    gd = ImageDraw.Draw(grad)
    for y in range(W):
        t = y / (W - 1)
        c = tuple(round(CREAM_TOP[i] + (CREAM_BOT[i] - CREAM_TOP[i]) * t) for i in range(3))
        gd.line([(0, y), (W, y)], fill=c + (255,))
    mask = Image.new("L", (W, W), 0)
    md = ImageDraw.Draw(mask)
    pad = int(W * 0.045)
    md.rounded_rectangle([pad, pad, W - pad, W - pad], radius=int(W * 0.23), fill=255)
    img.paste(grad, (0, 0), mask)

    d = ImageDraw.Draw(img)
    # 火焰主体（暖橙）
    outer = flame_points(W / 2, W / 2 + W * 0.02, W * 0.62)
    d.polygon(outer, fill=ORANGE + (255,))
    # 内焰（琥珀）
    inner = flame_points(W / 2 + W * 0.005, W / 2 + W * 0.075, W * 0.33)
    d.polygon(inner, fill=AMBER + (255,))
    # 内焰高光（奶白）
    core = flame_points(W / 2 + W * 0.012, W / 2 + W * 0.115, W * 0.15)
    d.polygon(core, fill=(255, 246, 224, 255))

    img = img.resize((S, S), Image.LANCZOS)
    return img

if __name__ == "__main__":
    im = build()
    im.save("icon_new.png")
    im.save("niuma/src/app.ico", sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48), (64, 64), (128, 128), (256, 256)])
    print("ok", im.size)
