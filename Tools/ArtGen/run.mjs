// artgen.html 을 헤드리스 브라우저로 열어 스프라이트 PNG 와 art_manifest.json 을 Assets/_Game/Art 에 쓴다.
// 사용법: node Tools/ArtGen/run.mjs
import { execFile } from 'node:child_process';
import { mkdirSync, writeFileSync, existsSync, mkdtempSync, rmSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { fileURLToPath, pathToFileURL } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, '..', '..');
const outDir = join(root, 'Assets', '_Game', 'Art');

// Edge 는 Windows 에서 --dump-dom 결과를 표준 출력으로 넘기지 않아서 Chrome 을 먼저 쓴다
const browsers = [
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
  'C:/Program Files/Microsoft/Edge/Application/msedge.exe',
  '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
  '/usr/bin/google-chrome',
  '/usr/bin/chromium',
];
const browser = process.env.BROWSER || browsers.find(p => existsSync(p));
if (!browser) { console.error('Chrome 또는 Edge 를 찾지 못했습니다. BROWSER 환경 변수로 경로를 지정하세요.'); process.exit(1); }

const profile = mkdtempSync(join(tmpdir(), 'artgen-'));
const url = pathToFileURL(join(here, 'artgen.html')).href;
const args = ['--headless=new', '--disable-gpu', '--no-first-run', '--disable-extensions', `--user-data-dir=${profile}`, '--dump-dom', url];

execFile(browser, args, { maxBuffer: 512 * 1024 * 1024 }, (err, stdout) => {
  try { rmSync(profile, { recursive: true, force: true }); } catch {}
  if (err) { console.error(err); process.exit(1); }
  const m = stdout.match(/<pre id="out">([\s\S]*?)<\/pre>/);
  if (!m) { console.error('출력을 찾지 못했습니다'); process.exit(1); }
  const json = m[1].replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&amp;/g, '&');
  const { files, manifest } = JSON.parse(json);
  let count = 0;
  for (const [path, dataUrl] of Object.entries(files)) {
    const file = join(outDir, path);
    mkdirSync(dirname(file), { recursive: true });
    writeFileSync(file, Buffer.from(dataUrl.split(',')[1], 'base64'));
    count++;
  }
  // Unity JsonUtility 가 읽을 수 있게 배열로 쓴다
  const items = Object.entries(manifest).map(([path, m]) => ({ path, ...m }));
  writeFileSync(join(outDir, 'art_manifest.json'), JSON.stringify({ items }, null, 1));
  console.log(`${count}개 스프라이트를 ${outDir} 에 저장했습니다`);
});
