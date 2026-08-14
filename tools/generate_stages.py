"""STAGE MODE の初期配置データ (src/Tetris.Core/Stages.csv) を生成する。

決定論的な乱数（固定シード）で 20 面ぶんを作る。各行には必ず 1 マス以上の空きを残し、
ジェムは壁ブロックの一部を置き換える形で配置する。
"""

from __future__ import annotations

import random
from pathlib import Path

WIDTH = 10
HEIGHT = 20
STAGES = 20
OUT = Path(__file__).resolve().parent.parent / "src" / "Tetris.Core" / "Stages.csv"


def build_stage(number: int, rng: random.Random) -> list[str]:
    rows = [["0"] * WIDTH for _ in range(HEIGHT)]
    filled_rows = min(3 + (number - 1) // 2, 12)
    gem_budget = min(2 + number // 2, 12)
    gem_candidates: list[tuple[int, int]] = []

    for i in range(filled_rows):
        y = HEIGHT - 1 - i
        holes = rng.randint(2, 4)
        hole_xs = set(rng.sample(range(WIDTH), holes))
        for x in range(WIDTH):
            if x in hole_xs:
                continue
            rows[y][x] = "1"
            gem_candidates.append((x, y))

    rng.shuffle(gem_candidates)
    for x, y in gem_candidates[:gem_budget]:
        rows[y][x] = "2"

    return ["".join(row) for row in rows]


def main() -> None:
    rng = random.Random(20260814)
    lines: list[str] = [
        "# STAGE MODE 初期配置データ: 0=空, 1=壁ブロック, 2=ジェム",
        "# 1 ステージ = 20 行 x 10 列。空行または '# Stage n' でステージを区切る。",
    ]
    for number in range(1, STAGES + 1):
        lines.append("")
        lines.append(f"# Stage {number}")
        lines.extend(build_stage(number, rng))

    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote {OUT}")


if __name__ == "__main__":
    main()
