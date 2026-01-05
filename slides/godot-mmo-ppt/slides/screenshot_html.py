#!/usr/bin/env python3
"""Screenshot all HTML files to PNG using Playwright"""
import asyncio
from playwright.async_api import async_playwright
import os

async def screenshot_html_files():
    async with async_playwright() as p:
        browser = await p.chromium.launch()
        page = await browser.new_page(viewport={'width': 1920, 'height': 1080})

        for i in range(1, 10):
            html_file = f'slide{i}.html'
            png_file = f'slide{i}.png'

            if os.path.exists(html_file):
                file_url = f'file://{os.path.abspath(html_file)}'
                await page.goto(file_url)
                await page.wait_for_load_state('networkidle')
                await page.screenshot(path=png_file, full_page=True)
                print(f'Screenshot: {html_file} -> {png_file}')

        await browser.close()

if __name__ == '__main__':
    asyncio.run(screenshot_html_files())
