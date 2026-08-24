from PIL import Image
import os

OUT = "/mnt/user-data/outputs/sprites"
os.makedirs(OUT, exist_ok=True)

T     = (0, 0, 0, 0)
INK   = (8, 6, 16, 255)
DK    = (22, 20, 42, 255)
TRACK = (30, 26, 54, 255)
MD    = (44, 42, 74, 255)
LT    = (86, 84, 128, 255)
RIM   = (120, 130, 180, 255)
WHITE = (236, 244, 255, 255)

TEAM = {
    "azul":     {"d": (16, 60, 120, 255), "m": (36, 132, 214, 255),
                 "b": (60, 178, 255, 255), "n": (150, 244, 255, 255)},
    "vermelha": {"d": (110, 16, 54, 255), "m": (206, 36, 92, 255),
                 "b": (255, 62, 118, 255), "n": (255, 168, 200, 255)},
}


class C:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.p = [[T] * w for _ in range(h)]
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

    def img(self):
        im = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
        for y in range(self.h):
            for x in range(self.w):
                im.putpixel((x, y), self.p[y][x])
        return im


def save(c, name):
    im = c.img()
    im.save(os.path.join(OUT, name))
    return name, im.size


# ---------------- BARRA DE VIDA (32x8) ----------------
def hp_frame():
    c = C(32, 8)
    c.rect(0, 0, 31, 7, MD)
    c.rect(1, 1, 30, 6, DK)
    c.rect(2, 2, 29, 5, TRACK)
    for x in (9, 16, 23):                     # divisorias de segmento
        c.rect(x, 2, x, 5, (18, 16, 36, 255))
    for p in ((0,0),(1,0),(0,1),(31,0),(30,0),(31,1),
              (0,7),(0,6),(1,7),(31,7),(30,7),(31,6)):
        c.set(p[0], p[1], RIM)                # cantos
    return c


def hp_fill(team=None, ghost=False):
    c = C(28, 4)
    if ghost:                                  # barra fantasma do dano recente
        ramp = [(255, 255, 255, 255), (226, 230, 245, 255),
                (188, 194, 215, 255), (140, 146, 172, 255)]
    else:
        t = TEAM[team]
        ramp = [t["n"], t["b"], t["m"], t["d"]]
    for y in range(4):
        for x in range(28):
            c.set(x, y, ramp[y])
    for x in range(1, 28, 5):                  # brilho pontilhado
        c.set(x, 1, WHITE if ghost else TEAM[team]["n"])
    return c


# ---------------- NUMEROS DE DANO ----------------
GLYPHS = {
    "0": ["01110","10001","10011","10101","11001","10001","01110"],
    "1": ["00100","01100","00100","00100","00100","00100","01110"],
    "2": ["01110","10001","00001","00010","00100","01000","11111"],
    "3": ["11111","00010","00100","00010","00001","10001","01110"],
    "4": ["00010","00110","01010","10010","11111","00010","00010"],
    "5": ["11111","10000","11110","00001","00001","10001","01110"],
    "6": ["00110","01000","10000","11110","10001","10001","01110"],
    "7": ["11111","00001","00010","00100","01000","01000","01000"],
    "8": ["01110","10001","10001","01110","10001","10001","01110"],
    "9": ["01110","10001","10001","01111","00001","00010","01100"],
    "-": ["00000","00000","00000","01110","00000","00000","00000"],
    "+": ["00000","00100","00100","11111","00100","00100","00000"],
    "!": ["00100","00100","00100","00100","00100","00000","00100"],
    "x": ["00000","00000","10001","01010","00100","01010","10001"],
}
ORDER = list("0123456789-+!x")
CELL_W, CELL_H = 8, 10

INKS = {
    "branco":    {"m": (232, 240, 255, 255), "h": (255, 255, 255, 255), "s": (146, 160, 198, 255)},
    "critico":   {"m": (255, 208, 62, 255),  "h": (255, 248, 190, 255), "s": (224, 116, 22, 255)},
    "azul":      {"m": (60, 178, 255, 255),  "h": (170, 246, 255, 255), "s": (24, 92, 172, 255)},
    "vermelho":  {"m": (255, 62, 118, 255),  "h": (255, 178, 205, 255), "s": (168, 22, 74, 255)},
    "cura":      {"m": (78, 232, 148, 255),  "h": (186, 255, 214, 255), "s": (22, 132, 92, 255)},
}


def digit_sheet(pal):
    c = C(CELL_W * len(ORDER), CELL_H)
    solid = set()
    for i, ch in enumerate(ORDER):
        ox = i * CELL_W + 1
        g = GLYPHS[ch]
        for y, row in enumerate(g):
            for x, v in enumerate(row):
                if v == "1":
                    solid.add((ox + x, 1 + y, i))
    coords = {(x, y) for (x, y, _) in solid}
    tops = {}
    bots = {}
    for (x, y) in coords:
        tops[x] = min(tops.get(x, 99), y)
        bots[x] = max(bots.get(x, -1), y)
    # corpo solido, brilho so na aresta superior, sombra so na inferior
    for (x, y) in coords:
        if y == tops[x] and y <= 2:
            c.set(x, y, pal["h"])
        elif y == bots[x] and y >= 6:
            c.set(x, y, pal["s"])
        else:
            c.set(x, y, pal["m"])
    # contorno escuro em 8 direcoes, sem invadir a celula vizinha
    add = []
    for (x, y) in coords:
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                nx, ny = x + dx, y + dy
                if (nx, ny) in coords:
                    continue
                if 0 <= nx < c.w and 0 <= ny < c.h:
                    add.append((nx, ny))
    for (x, y) in add:
        c.set(x, y, INK)
    return c


made = []
made.append(save(hp_frame(), "barra_vida_moldura.png"))
made.append(save(hp_fill("azul"), "barra_vida_azul.png"))
made.append(save(hp_fill("vermelha"), "barra_vida_vermelha.png"))
made.append(save(hp_fill(ghost=True), "barra_vida_dano_recente.png"))
for name, pal in INKS.items():
    made.append(save(digit_sheet(pal), f"numeros_{name}.png"))

print("\n".join(f"{n}  {s}" for n, s in made))
