#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Скрипт: собрать код из выбранных подпапок и расширений в один файл.

Примеры:
  python collect_code.py --root . --dirs TaskReviewPlatform --ext cs csproj json yml yaml --out merged_code.txt
  python collect_code.py --root . --dirs TaskReviewPlatform/Backend TaskReviewPlatform/Frontend --ext cs ts tsx js json --out appendix.txt
"""

from __future__ import annotations

import argparse
import os
from pathlib import Path
from datetime import datetime


DEFAULT_EXCLUDES = {
    ".git", ".idea", ".vs", ".vscode",
    "bin", "obj",
    "node_modules",
    "dist", "build", ".next",
    "__pycache__",
    ".pytest_cache",
}


def normalize_ext_list(exts: list[str]) -> set[str]:
    """Превратить ['cs', '.json'] в {'.cs', '.json'}"""
    out = set()
    for e in exts:
        e = e.strip()
        if not e:
            continue
        if not e.startswith("."):
            e = "." + e
        out.add(e.lower())
    return out


def should_skip_dir(dir_path: Path, exclude_dirs: set[str]) -> bool:
    return dir_path.name in exclude_dirs


def iter_files(root: Path, dirs: list[Path], exts: set[str], exclude_dirs: set[str]) -> list[Path]:
    files: list[Path] = []
    for d in dirs:
        base = (root / d).resolve()
        if not base.exists():
            print(f"[WARN] Папка не найдена: {base}")
            continue
        if base.is_file():
            # если случайно передали файл вместо папки
            if base.suffix.lower() in exts:
                files.append(base)
            continue

        for current_root, subdirs, filenames in os.walk(base):
            current_root_path = Path(current_root)

            # фильтрация подпапок прямо в os.walk (ускоряет)
            subdirs[:] = [sd for sd in subdirs if sd not in exclude_dirs]

            for fn in filenames:
                p = current_root_path / fn
                if p.suffix.lower() in exts:
                    files.append(p)
    files = sorted(set(files), key=lambda x: str(x).lower())
    return files


def safe_read_text(path: Path) -> str:
    # Пробуем utf-8, если не вышло — читаем "как получится"
    try:
        return path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        return path.read_text(encoding="utf-8", errors="replace")


def main() -> int:
    parser = argparse.ArgumentParser(description="Собрать код из подпапок в один файл.")
    parser.add_argument("--root", default=".", help="Корень проекта (по умолчанию текущая папка).")
    parser.add_argument("--dirs", nargs="+", required=True,
                        help="Список подпапок (относительно --root), откуда собирать (рекурсивно).")
    parser.add_argument("--ext", nargs="+", required=True,
                        help="Расширения файлов, например: cs csproj json yml yaml")
    parser.add_argument("--out", default="merged_code.txt", help="Выходной файл.")
    parser.add_argument("--exclude-dirs", nargs="*", default=sorted(DEFAULT_EXCLUDES),
                        help="Какие папки пропускать (по имени). По умолчанию исключает bin/obj/node_modules и т.п.")
    parser.add_argument("--max-bytes", type=int, default=2_000_000,
                        help="Макс. размер одного файла в байтах (по умолчанию 2MB), чтобы случайно не слить гигантские артефакты.")
    parser.add_argument("--include-empty", action="store_true",
                        help="Включать пустые файлы (по умолчанию пустые пропускаются).")

    args = parser.parse_args()

    root = Path(args.root).resolve()
    dir_list = [Path(d) for d in args.dirs]
    exts = normalize_ext_list(args.ext)
    exclude_dirs = set(args.exclude_dirs)

    out_path = (root / args.out).resolve()

    files = iter_files(root, dir_list, exts, exclude_dirs)

    header = []
    header.append("СБОРНИК ИСХОДНОГО КОДА (auto-generated)")
    header.append(f"Дата: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    header.append(f"Root: {root}")
    header.append(f"Dirs: {', '.join(map(str, args.dirs))}")
    header.append(f"Exts: {', '.join(sorted(exts))}")
    header.append(f"Exclude dirs: {', '.join(sorted(exclude_dirs))}")
    header.append(f"Всего файлов: {len(files)}")
    header.append("\n" + "=" * 120 + "\n")

    written = 0
    skipped_big = 0
    skipped_empty = 0

    with out_path.open("w", encoding="utf-8", newline="\n") as out:
        out.write("\n".join(header))

        for p in files:
            try:
                size = p.stat().st_size
            except OSError:
                continue

            if size > args.max_bytes:
                skipped_big += 1
                continue

            if size == 0 and not args.include_empty:
                skipped_empty += 1
                continue

            rel = p.relative_to(root)

            out.write(f"\n/* {'#' * 110}\n")
            out.write(f"   FILE: {rel}\n")
            out.write(f"   SIZE: {size} bytes\n")
            out.write(f"{'#' * 110} */\n\n")

            out.write(safe_read_text(p))
            if not safe_read_text(p).endswith("\n"):
                out.write("\n")

            out.write("\n" + "-" * 120 + "\n")
            written += 1

        out.write("\n" + "=" * 120 + "\n")
        out.write(f"ИТОГО: записано файлов: {written}, пропущено (слишком большие): {skipped_big}, пропущено (пустые): {skipped_empty}\n")

    print(f"[OK] Готово: {out_path}")
    print(f"     Записано файлов: {written}")
    print(f"     Пропущено больших: {skipped_big}")
    print(f"     Пропущено пустых: {skipped_empty}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
