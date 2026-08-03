import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1400, height: 900 } });
await page.setViewportSize({ width: 1400, height: 900 });
await page.goto('http://127.0.0.1:8991/repro.html');
await page.waitForTimeout(500);

const rect = await page.evaluate(() => {
  const el = document.getElementById('backdrop');
  const r = el.getBoundingClientRect();
  const cs = getComputedStyle(el);
  return {
    rect: { top: r.top, left: r.left, right: r.right, bottom: r.bottom, width: r.width, height: r.height },
    position: cs.position,
    zIndex: cs.zIndex,
    viewport: { w: window.innerWidth, h: window.innerHeight },
  };
});
console.log(JSON.stringify(rect, null, 2));

await page.screenshot({ path: 'C:/Users/vobfr/AppData/Local/Temp/claude/d--OneDrive-ReplayStudio-Clientes-004-DonaBetinha/facc3362-a345-49b1-970e-7650819f5877/scratchpad/before.png' });
await browser.close();
