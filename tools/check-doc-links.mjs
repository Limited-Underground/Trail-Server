import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, extname, join, normalize, resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const roots = ["README.md", "CONTRIBUTING.md", "SECURITY.md", "docs", "tasks"];
const markdownFiles = [];

function collect(relativePath) {
  const absolutePath = join(root, relativePath);
  if (!existsSync(absolutePath)) return;
  if (statSync(absolutePath).isDirectory()) {
    for (const entry of readdirSync(absolutePath)) collect(join(relativePath, entry));
    return;
  }
  if (extname(absolutePath).toLowerCase() === ".md") markdownFiles.push(absolutePath);
}

for (const entry of roots) collect(entry);

const failures = [];
const linkPattern = /(?<!!)\[[^\]]*\]\(([^)]+)\)/g;

for (const file of markdownFiles) {
  const contents = readFileSync(file, "utf8");
  for (const match of contents.matchAll(linkPattern)) {
    const rawTarget = match[1].trim().replace(/^<|>$/g, "");
    if (!rawTarget || rawTarget.startsWith("#") || /^[a-z][a-z0-9+.-]*:/i.test(rawTarget)) continue;
    const pathTarget = decodeURIComponent(rawTarget.split("#", 1)[0]);
    const target = normalize(resolve(dirname(file), pathTarget));
    if (!target.startsWith(root) || !existsSync(target)) {
      failures.push(`${file.slice(root.length + 1)} -> ${rawTarget}`);
    }
  }
}

if (failures.length > 0) {
  console.error("Broken or out-of-scope documentation links:");
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}

console.log(`PASS: ${markdownFiles.length} Markdown files have valid local links`);
