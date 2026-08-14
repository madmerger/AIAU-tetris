"""ステージ初期配置データ (src/Tetris.Core/Data/stages.csv) の生成スクリプト。

面ごとに決定的な乱数シードを使い、下部に壁ブロック・ジェムを配置した 20 面を生成する。
各行には必ず 2 マス以上の空きを残し、初期状態で埋まっている行が出来ないようにする。
"""

import os
import random

WIDTH = 10
HEIGHT = 20
STAGES = 20
OUT = os.path.join(os.path.dirname(__file__), "..", "src", "Tetris.Core", "Data", "stages.csv")


def build_stage(index: int) -> list[list[int]]:
    rng = random.Random(1000 + index)
    grid = [[0] * WIDTH for _ in range(HEIGHT)]
    floor = 2 + index // 3            # 埋める行数: 2..8
    gems = 1 + index // 4             # ジェム数: 1..5
    filled_rows = list(range(HEIGHT - floor, HEIGHT))

    for row in filled_rows:
        depth = row - (HEIGHT - floor)
        density = min(0.75, 0.35 + 0.08 * index / 2 + 0.05 * depth)
        count = max(1, min(WIDTH - 2, round(WIDTH * density)))
        columns = rng.sample(range(WIDTH), count)
        for col in columns:
            grid[row][col] = 1

    wall_positions = [(r, c) for r in filled_rows for c in range(WIDTH) if grid[r][c] == 1]
    for row, col in rng.sample(wall_positions, min(gems, len(wall_positions))):
        grid[row][col] = 2

    for row in filled_rows:
        assert grid[row].count(0) >= 2, f"stage {index + 1} row {row} が埋まりすぎです"
    assert sum(r.count(2) for r in grid) == gems
    return grid


def main() -> None:
    lines = [
        "# ステージ初期配置データ (0=空, 1=壁ブロック, 2=ジェム)",
        "# tools/gen_stages.py により生成。1 面 = 20 行 x 10 列。",
    ]
    for index in range(STAGES):
        lines.append(f"# stage {index + 1}")
        for row in build_stage(index):
            lines.append(",".join(str(v) for v in row))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("\n".join(lines) + "\n")
    print(f"wrote {OUT}")


if __name__ == "__main__":
    main()
