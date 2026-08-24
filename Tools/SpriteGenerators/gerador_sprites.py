from PIL import Image, ImageDraw, ImageFont
import os, math

OUT = "/mnt/user-data/outputs/sprites"
os.makedirs(OUT, exist_ok=True)

# ---------------- paleta cyberpunk ----------------
T = (0, 0, 0, 0)
INK   = (8, 6, 16, 255)        # contorno externo
DK    = (22, 20, 42, 255)      # metal escuro
MD    = (44, 42, 74, 255)      # metal medio
LT    = (86, 84, 128, 255)     # metal claro
WHITE = (236, 244, 255, 255)

BLUE = {
    "d": (10, 34, 74, 255),
    "m": (24, 92, 172, 255),
    "b": (60, 178, 255, 255),
    "n": (150, 244, 255, 255),
}
RED = {
    "d": (72, 10, 40, 255),
    "m": (176, 26, 78, 255),
    "b": (255, 62, 118, 255),
    "n": (255, 168, 200, 255),
}
ESS = {
    "d": (48, 16, 76, 255),
    "m": (138, 46, 208, 255),
    "b": (196, 112, 255, 255),
    "n": (238, 206, 255, 255),
}

# ---------------- canvas helpers ----------------
class C:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.p = [[T for _ in range(w)] for _ in range(h)]

    def set(self, x, y, c):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h and c[3] > 0:
            self.p[y][x] = c

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.p[y][x]
        return T

    def rect(self, x0, y0, x1, y1, c):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.set(x, y, c)

    def disc(self, cx, cy, r, c):
        for y in range(self.h):
            for x in range(self.w):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                    self.set(x, y, c)

    def outline(self, c=INK):
        """Contorno escuro em volta de tudo que ja foi desenhado."""
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


def save(canvas, name):
    im = canvas.img()
    im.save(os.path.join(OUT, name))
    return im


# ---------------- ESCUDO ----------------
def shield(team):
    c = C(32, 32)
    cx, top, bot, base = 15.5, 4, 28, 10.0

    def half_at(y):
        t = (y - top) / (bot - top)
        if t < 0.45:
            h = base
        else:
            u = (t - 0.45) / 0.55
            h = base * (1.0 - u ** 1.7)
        if y < top + 2:                     # canto superior arredondado
            h -= (top + 2 - y) * 1.6
        return h

    mask = set()
    for y in range(top, bot + 1):
        h = half_at(y)
        for x in range(32):
            if abs(x - cx) <= h:
                mask.add((x, y))

    # corpo: gradiente vertical escuro -> medio
    for (x, y) in mask:
        t = (y - top) / (bot - top)
        c.set(x, y, team["d"] if t > 0.45 else team["m"])

    # borda neon (pixels da mascara que tocam o vazio)
    for (x, y) in mask:
        for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
            if (x+dx, y+dy) not in mask:
                c.set(x, y, team["b"]); break

    # nucleo: losango brilhante
    ccx, ccy = 15.5, 14.5
    for y in range(32):
        for x in range(32):
            d = abs(x - ccx) + abs(y - ccy)
            if (x, y) in mask:
                if d <= 2.2:
                    c.set(x, y, WHITE)
                elif d <= 4.2:
                    c.set(x, y, team["n"])
                elif d <= 5.6:
                    c.set(x, y, team["b"])

    # circuito: entalhes verticais nas laterais + trilha inferior
    for y in range(10, 19):
        for x in (8, 23):
            if (x, y) in mask:
                c.set(x, y, team["b"] if y % 3 else team["n"])
    for x in range(13, 19):
        if (x, 22) in mask:
            c.set(x, 22, team["b"])
    for y in range(19, 23):
        if (15, y) in mask: c.set(15, y, team["m"])
        if (16, y) in mask: c.set(16, y, team["m"])

    c.outline()
    return c


# ---------------- BOLA ----------------
def ball():
    c = C(16, 16)
    cx = cy = 7.5
    for y in range(16):
        for x in range(16):
            d = math.hypot(x - cx, y - cy)
            if d <= 2.0:
                c.set(x, y, WHITE)
            elif d <= 4.0:
                c.set(x, y, (170, 246, 255, 255))
            elif d <= 5.6:
                c.set(x, y, (58, 176, 246, 255))
            elif d <= 6.6:
                c.set(x, y, (30, 96, 190, 255))
    # brilho superior esquerdo
    c.set(5, 4, WHITE); c.set(6, 4, WHITE); c.set(5, 5, WHITE)
    # faiscas
    for p in ((7, 1), (8, 1), (14, 7), (14, 8), (7, 14), (8, 14), (1, 7), (1, 8)):
        c.set(p[0], p[1], (150, 230, 255, 255))
    c.outline()
    return c


# ---------------- TORRES ----------------
def tower(team, tier):
    c = C(32, 32)
    d, m, b, n = team["d"], team["m"], team["b"], team["n"]

    def core(x0, y0, x1, y1):
        c.rect(x0, y0, x1, y1, d)
        c.rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, b)
        c.rect(x0 + 2, y0 + 2, x1 - 2, y1 - 2, n)

    if tier == 1:
        c.rect(8, 26, 23, 29, DK)          # base
        c.rect(9, 26, 22, 26, MD)
        c.rect(9, 28, 10, 28, m)
        c.rect(21, 28, 22, 28, m)
        c.rect(11, 17, 20, 26, MD)         # corpo
        c.rect(12, 18, 19, 25, DK)
        c.rect(11, 17, 11, 26, LT)
        c.rect(9, 20, 10, 24, DK)          # aletas laterais
        c.rect(21, 20, 22, 24, DK)
        c.rect(9, 21, 9, 23, b)
        c.rect(22, 21, 22, 23, b)
        core(13, 19, 18, 24)
        c.rect(13, 14, 18, 17, MD)         # cabeca
        c.rect(14, 15, 17, 16, DK)
        c.rect(15, 12, 16, 15, b)          # emissor
        c.rect(15, 12, 16, 12, n)

    elif tier == 2:
        c.rect(6, 25, 25, 29, DK)
        c.rect(7, 25, 24, 25, MD)
        c.rect(7, 27, 8, 28, m)
        c.rect(23, 27, 24, 28, m)
        c.rect(10, 14, 21, 25, MD)         # corpo
        c.rect(11, 15, 20, 24, DK)
        c.rect(10, 14, 10, 25, LT)
        c.rect(7, 17, 9, 23, DK)           # aletas
        c.rect(22, 17, 24, 23, DK)
        c.rect(8, 18, 8, 22, b)
        c.rect(23, 18, 23, 22, b)
        core(13, 17, 18, 22)
        c.rect(11, 9, 20, 14, MD)          # cabeca
        c.rect(12, 10, 19, 13, DK)
        c.rect(13, 11, 18, 11, b)
        c.rect(13, 7, 14, 10, MD)          # canos
        c.rect(17, 7, 18, 10, MD)
        c.rect(13, 6, 14, 6, n)
        c.rect(17, 6, 18, 6, n)

    else:
        c.rect(4, 24, 27, 29, DK)          # base larga
        c.rect(5, 24, 26, 24, MD)
        c.rect(5, 26, 7, 28, m)
        c.rect(24, 26, 26, 28, m)
        c.rect(8, 21, 10, 24, DK)          # contrafortes
        c.rect(21, 21, 23, 24, DK)
        c.rect(11, 11, 20, 24, MD)         # corpo
        c.rect(12, 12, 19, 23, DK)
        c.rect(11, 11, 11, 24, LT)
        core(13, 15, 18, 21)
        c.rect(13, 13, 18, 13, b)
        c.rect(6, 15, 10, 20, DK)          # asas
        c.rect(21, 15, 25, 20, DK)
        c.rect(7, 16, 7, 19, b)
        c.rect(24, 16, 24, 19, b)
        c.rect(8, 17, 9, 18, m)
        c.rect(22, 17, 23, 18, m)
        c.rect(9, 6, 22, 11, MD)           # coroa
        c.rect(10, 7, 21, 10, DK)
        c.rect(11, 8, 20, 9, m)
        c.rect(13, 8, 18, 9, b)
        c.rect(9, 6, 22, 6, LT)
        c.rect(9, 3, 10, 6, MD)            # torres laterais
        c.rect(21, 3, 22, 6, MD)
        c.rect(9, 2, 10, 2, n)
        c.rect(21, 2, 22, 2, n)
        c.rect(15, 1, 16, 6, b)            # antena central
        c.rect(15, 0, 16, 0, WHITE)

    c.outline()
    return c


# ---------------- BARRA DE ESSENCIA ----------------
def bar_frame():
    c = C(64, 12)
    c.rect(1, 1, 62, 10, DK)               # moldura
    c.rect(2, 2, 61, 9, (26, 22, 48, 255)) # trilho interno
    c.rect(0, 0, 63, 0, MD)
    c.rect(0, 11, 63, 11, MD)
    c.rect(0, 0, 0, 11, MD)
    c.rect(63, 0, 63, 11, MD)
    for x in range(2, 62):                 # marcas de segmento
        if (x - 2) % 10 == 0 and x > 2:
            c.rect(x, 3, x, 8, (32, 28, 60, 255))
    for p in ((0,0),(1,0),(0,1),(62,0),(63,0),(63,1),
              (0,10),(0,11),(1,11),(63,10),(62,11),(63,11)):
        c.set(p[0], p[1], ESS["b"])        # cantos neon
    return c


def bar_fill():
    c = C(60, 8)
    for x in range(60):
        c.rect(x, 0, x, 0, ESS["b"])
        c.rect(x, 1, x, 2, ESS["n"] if x % 6 == 2 else ESS["b"])
        c.rect(x, 3, x, 5, ESS["m"])
        c.rect(x, 6, x, 7, ESS["d"])
    return c


# ---------------- gerar tudo ----------------
files = []
files.append(("escudo_azul.png", save(shield(BLUE), "escudo_azul.png")))
files.append(("escudo_vermelho.png", save(shield(RED), "escudo_vermelho.png")))
files.append(("bola.png", save(ball(), "bola.png")))
for i in (1, 2, 3):
    files.append((f"torre_azul_{i}.png", save(tower(BLUE, i), f"torre_azul_{i}.png")))
for i in (1, 2, 3):
    files.append((f"torre_vermelha_{i}.png", save(tower(RED, i), f"torre_vermelha_{i}.png")))
files.append(("barra_essencia_moldura.png", save(bar_frame(), "barra_essencia_moldura.png")))
files.append(("barra_essencia_preenchimento.png", save(bar_fill(), "barra_essencia_preenchimento.png")))

print("\n".join(f"{n}  {im.size}" for n, im in files))
