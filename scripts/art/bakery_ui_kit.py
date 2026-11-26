#!/usr/bin/env python3
"""Generates Slothy's Bakery UI kit, Practice topic art, and Achievement pastry badges.
Delegates shared 9-slice containers and shell icons to CurrentlyWorkin/scripts/art/generate_slothy_ui_kit.py.
Outputs:
  Documentation/ArtSource/ui/*.svg
  Assets/Resources/BakeryUI/*.png
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
BAKERY_ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
SHARED_ART = os.path.abspath(os.path.join(BAKERY_ROOT, "..", "scripts", "art"))
if SHARED_ART not in sys.path:
    sys.path.insert(0, SHARED_ART)

import generate_slothy_ui_kit as kit

THEME = dict(kit.DEFAULT_THEME)
INK = THEME["ink"]
PINK = THEME["accent_top"]
BERRY = THEME["accent_bot"]
CREAM = THEME["cream"]
STRIPE = THEME["stripe"]
MINT = THEME["mint"]
GOLD = THEME["gold"]
CARAMEL = THEME["caramel"]


def wrap(inner):
    return kit.wrap_svg(
        128,
        128,
        f'<g stroke="{INK}" stroke-width="6.5" stroke-linejoin="round" stroke-linecap="round">{inner}</g>',
    )


def bakery_extras():
    return {
        # Practice topic illustrations (MG1, MG2, MG3)
        "topic_mg1": (
            wrap(
                f"""
                <circle cx="40" cy="56" r="24" fill="#ECE7EA"/>
                <path d="M40 32A24 24 0 0 1 40 80Z" fill="#9EE0AE"/>
                <circle cx="88" cy="56" r="24" fill="#ECE7EA"/>
                <path d="M88 32A24 24 0 1 1 64 56H88Z" fill="#9EE0AE"/>
                <path d="M40 32v48M88 32v48M64 56h48" stroke-width="4" fill="none"/>
                <text x="40" y="106" text-anchor="middle" font-family="sans-serif" font-weight="800" font-size="22" fill="{INK}" stroke="none">1/2</text>
                <text x="88" y="106" text-anchor="middle" font-family="sans-serif" font-weight="800" font-size="22" fill="{BERRY}" stroke="none">3/4</text>
                """
            ),
            128,
            128,
        ),
        "topic_mg2": (
            wrap(
                f"""
                <circle cx="32" cy="56" r="21" fill="{CREAM}"/>
                <circle cx="96" cy="56" r="21" fill="{CREAM}"/>
                <rect x="48" y="42" width="32" height="28" rx="9" fill="{STRIPE}"/>
                <path d="M70 49l-12 7 12 7" stroke="#D94B4B" stroke-width="6" fill="none"/>
                <text x="32" y="102" text-anchor="middle" font-family="sans-serif" font-weight="800" font-size="21" fill="{INK}" stroke="none">1/5</text>
                <text x="96" y="102" text-anchor="middle" font-family="sans-serif" font-weight="800" font-size="21" fill="{INK}" stroke="none">1/3</text>
                """
            ),
            128,
            128,
        ),
        "topic_mg3": (
            wrap(
                f"""
                <rect x="20" y="34" width="44" height="32" rx="10" fill="#9BE089"/>
                <rect x="64" y="34" width="44" height="32" rx="10" fill="#6F666C" fill-opacity="0.45"/>
                <circle cx="34" cy="48" r="5" fill="#EA5A6E" stroke="none"/>
                <circle cx="50" cy="48" r="5" fill="#EA5A6E" stroke="none"/>
                <rect x="24" y="80" width="80" height="26" rx="9" fill="#FFFFFF"/>
                <text x="64" y="100" text-anchor="middle" font-family="sans-serif" font-weight="800" font-size="20" fill="{BERRY}" stroke="none">1/2 = 2/4</text>
                """
            ),
            128,
            128,
        ),
        # Bakery pastry achievement badges
        "ach_order": (
            wrap(
                f"""
                <path d="M36 68h56l-8 34H44z" fill="{STRIPE}"/>
                <path d="M30 68c0-18 14-30 34-30s34 12 34 30z" fill="{PINK}"/>
                <circle cx="64" cy="28" r="9" fill="#E6526E"/>
                """
            ),
            128,
            128,
        ),
        "ach_tray": (
            wrap(
                f"""
                <rect x="20" y="64" width="88" height="34" rx="12" fill="{CREAM}"/>
                <ellipse cx="44" cy="56" rx="16" ry="12" fill="{GOLD}"/>
                <ellipse cx="84" cy="56" rx="16" ry="12" fill="{GOLD}"/>
                <ellipse cx="64" cy="46" rx="16" ry="12" fill="#F6C873"/>
                """
            ),
            128,
            128,
        ),
        "ach_perfect": (
            wrap(
                f"""
                <path d="M64 20l12 25 28 4-20 19 5 28-25-13-25 13 5-28-20-19 28-4z" fill="{GOLD}"/>
                <circle cx="64" cy="62" r="12" fill="{MINT}" stroke-width="5"/>
                """
            ),
            128,
            128,
        ),
        "ach_recipe": (
            wrap(
                f"""
                <rect x="28" y="24" width="72" height="80" rx="14" fill="{CREAM}"/>
                <path d="M44 46h40M44 64h40M44 82h26" stroke="{BERRY}" fill="none"/>
                """
            ),
            128,
            128,
        ),
        "ach_all_topics": (
            wrap(
                f"""
                <circle cx="44" cy="48" r="18" fill="{PINK}"/>
                <circle cx="84" cy="48" r="18" fill="{GOLD}"/>
                <circle cx="64" cy="80" r="18" fill="#9EE0AE"/>
                """
            ),
            128,
            128,
        ),
        "ach_daily": (
            wrap(
                f"""
                <rect x="24" y="32" width="80" height="72" rx="16" fill="{CREAM}"/>
                <path d="M24 54h80M46 22v18M82 22v18" fill="none"/>
                <path d="M46 78l12 12 24-26" stroke="{MINT}" stroke-width="9" fill="none"/>
                """
            ),
            128,
            128,
        ),
        "ach_daily5": (
            wrap(
                f"""
                <path d="M64 18c8 22 32 34 32 60a32 32 0 0 1-64 0c0-15 8-24 14-31 2 10 7 17 13 19-5-16 0-34 5-48z" fill="#EE863D"/>
                <circle cx="64" cy="80" r="14" fill="{GOLD}" stroke-width="5"/>
                """
            ),
            128,
            128,
        ),
        "ach_shift": (
            wrap(
                f"""
                <circle cx="64" cy="66" r="38" fill="#E9B277"/>
                <path d="M64 66c0-10 12-10 12 0 0 14-24 14-24 0 0-20 34-20 34 0" fill="none" stroke="{CARAMEL}"/>
                """
            ),
            128,
            128,
        ),
        "ach_chef": (
            wrap(
                f"""
                <path d="M34 74a20 20 0 0 1-3-39 26 26 0 0 1 33-15 26 26 0 0 1 33 15 20 20 0 0 1-3 39z" fill="{CREAM}"/>
                <rect x="36" y="74" width="56" height="26" rx="6" fill="{STRIPE}"/>
                <path d="M52 80v14M64 80v14M76 80v14" stroke-width="5" fill="none"/>
                """
            ),
            128,
            128,
        ),
        "ach_days": (
            wrap(
                f"""
                <circle cx="64" cy="64" r="38" fill="{CARAMEL}"/>
                <circle cx="64" cy="64" r="28" fill="{PINK}"/>
                <circle cx="64" cy="64" r="12" fill="{CREAM}"/>
                """
            ),
            128,
            128,
        ),
    }


def main():
    svg_dir = os.path.join(BAKERY_ROOT, "Documentation", "ArtSource", "ui")
    png_dir = os.path.join(BAKERY_ROOT, "Assets", "Resources", "BakeryUI")
    svg_only = "--svg-only" in sys.argv
    items = kit.build_shared_items(THEME)
    items.update(bakery_extras())
    kit.export_items(svg_dir, png_dir, items, svg_only=svg_only)


if __name__ == "__main__":
    main()
