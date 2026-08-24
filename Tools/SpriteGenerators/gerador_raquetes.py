from PIL import Image
import os

OUT = "/mnt/user-data/outputs/sprites"
os.makedirs(OUT, exist_ok=True)

T     = (0, 0, 0, 0)
INK   = (8, 6, 16, 255)
DK    = (22, 20, 42, 255)
MD    = (44, 42, 74, 255)
LT    = (86, 84, 128, 255)
WHITE = (236, 244, 255, 255)

TEAM = {
    "azul":     {"d": (10, 34, 74, 255),  "m": (24, 92, 172, 255),
                 "b": (60, 178, 255, 255), "n": (150, 244, 255, 255)},
    "vermelha": {"d": (72, 10, 40, 255),  "m": (176, 26, 78, 255),
                 "b": (255, 62, 118, 255), "n": (255, 168, 200, 255)},
}


class C:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.p = [[T for _ in range(w)] for _ in range(h)]

    def set(self, x, y, c):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h and c[3] > 0:
            self.p[y][x] = c

    def get(self, x, y):
        return self.p[y][x] if 0 <= x < self.w and 0 <= y < self.h else T

    def rect(self, x0, y0, x1, y1, c):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.set(x, y, c)

    def outline(self, c=INK):
        add = []
        for y in range(self.h):
            for x in range(self.w):
                if self.get(x, y)[3] == 0:
                    for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
                        if self.get(x+dx, y+dy)[3] > 0:
                            add.append((x, y)); break
        for x, y in add:
            self.set(x, y, c)

    def img(self):
        im = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
        for y in range(self.h):
            for x in range(self.w):
                im.putpixel((x, y), self.p[y][x])
        return im


def span(x, w):
    """Perfil da raquete: pontas afiladas, meio com altura cheia."""
    d = min(x, w - 1 - x)
    if d < 2:   return 4, 7
    if d < 4:   return 3, 8
    if d < 6:   return 2, 9
    return 1, 10


def paddle(team, w=48, flash=False):
    c = C(w, 12)
    t = TEAM[team]
    for x in range(w):
        top, bot = span(x, w)
        for y in range(top, bot + 1):
            depth = min(y - top, bot - y)
            if flash:
                col = WHITE if depth <= 1 else t["n"]
            else:
                col = [t["n"], t["b"], DK, DK, MD][min(depth, 4)]
            c.set(x, y, col)

    # celulas de energia, espelhadas a partir do centro
    mx = w // 2
    cells = []
    k = 1
    while True:
        a, b = mx - 8 * k, mx + 8 * k
        if a - 1 < 6 or b + 1 > w - 7:
            break
        cells += [a, b]
        k += 1
    for cx in cells:
        c.rect(cx - 1, 4, cx + 1, 7, WHITE if flash else t["b"])
        c.rect(cx - 1, 5, cx + 1, 6, WHITE if flash else t["n"])

    # nucleo central, um pouco maior
    c.rect(mx - 2, 3, mx + 1, 8, WHITE if flash else t["m"])
    c.rect(mx - 2, 4, mx + 1, 7, WHITE if flash else t["b"])
    c.rect(mx - 1, 5, mx + 0, 6, WHITE if flash else t["n"])

    # brilho de aresta nas duas faces (funciona em cima e embaixo)
    for x in range(6, w - 6):
        c.set(x, 1, t["n"])
        c.set(x, 10, t["n"])

    c.outline()
    return c


made = []
WIDTHS = {"curta": 32, "": 48, "longa": 64}
for team in ("azul", "vermelha"):
    for suf, w in WIDTHS.items():
        name = f"raquete_{team}{'_' + suf if suf else ''}.png"
        paddle(team, w).img().save(os.path.join(OUT, name))
        made.append((name, (w, 12)))
    fn = f"raquete_{team}_impacto.png"
    paddle(team, 48, flash=True).img().save(os.path.join(OUT, fn))
    made.append((fn, (48, 12)))

print("\n".join(f"{n}  {s}" for n, s in made))
