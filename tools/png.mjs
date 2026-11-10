// Just enough PNG for screenshots: read 8-bit RGB/RGBA (non-interlaced), write 8-bit RGB.
import { readFileSync, writeFileSync } from "node:fs";
import { deflateSync, inflateSync } from "node:zlib";

export function readPng(source) {
  const b = Buffer.isBuffer(source) ? source : readFileSync(source);
  let o = 8, w, h, ct;
  const idat = [];
  while (o < b.length) {
    const len = b.readUInt32BE(o), type = b.toString("ascii", o + 4, o + 8), d = b.subarray(o + 8, o + 8 + len);
    if (type === "IHDR") { w = d.readUInt32BE(0); h = d.readUInt32BE(4); ct = d[9]; }
    if (type === "IDAT") idat.push(d);
    o += 12 + len;
  }
  const bpp = ct === 6 ? 4 : 3, raw = inflateSync(Buffer.concat(idat)), stride = w * bpp, px = Buffer.alloc(h * stride);
  for (let y = 0; y < h; y++) {
    const f = raw[y * (stride + 1)], src = raw.subarray(y * (stride + 1) + 1, (y + 1) * (stride + 1));
    for (let x = 0; x < stride; x++) {
      const a = x >= bpp ? px[y * stride + x - bpp] : 0, up = y ? px[(y - 1) * stride + x] : 0, c = y && x >= bpp ? px[(y - 1) * stride + x - bpp] : 0;
      let v = src[x];
      if (f === 1) v += a; else if (f === 2) v += up; else if (f === 3) v += (a + up) >> 1;
      else if (f === 4) { const p = a + up - c, pa = Math.abs(p - a), pb = Math.abs(p - up), pc = Math.abs(p - c); v += pa <= pb && pa <= pc ? a : pb <= pc ? up : c; }
      px[y * stride + x] = v & 255;
    }
  }
  return { w, h, rgb: (x, y) => { const i = y * stride + x * bpp; return [px[i], px[i + 1], px[i + 2]]; } };
}

const CRC = new Int32Array(256).map((_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c; });
const crc32 = buf => { let c = -1; for (const byte of buf) c = CRC[(c ^ byte) & 255] ^ (c >>> 8); return (c ^ -1) >>> 0; };
function chunk(type, data) {
  const out = Buffer.alloc(12 + data.length);
  out.writeUInt32BE(data.length, 0); out.write(type, 4, "ascii"); data.copy(out, 8);
  out.writeUInt32BE(crc32(out.subarray(4, 8 + data.length)), 8 + data.length);
  return out;
}

// Stacks images top to bottom on a white sheet with a gap between them.
export function writeStacked(path, images, gap = 6) {
  const w = Math.max(...images.map(i => i.w)), h = images.reduce((a, i) => a + i.h, 0) + gap * (images.length - 1);
  const raw = Buffer.alloc(h * (w * 3 + 1), 255);
  let top = 0;
  for (const img of images) {
    for (let y = 0; y < img.h; y++) {
      const row = (top + y) * (w * 3 + 1);
      raw[row] = 0;
      for (let x = 0; x < img.w; x++) { const [r, g, b] = img.rgb(x, y); raw[row + 1 + x * 3] = r; raw[row + 2 + x * 3] = g; raw[row + 3 + x * 3] = b; }
    }
    top += img.h + gap;
  }
  for (let y = 0; y < h; y++) raw[y * (w * 3 + 1)] = 0;
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 2;
  writeFileSync(path, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ihdr), chunk("IDAT", deflateSync(raw)), chunk("IEND", Buffer.alloc(0))]));
}
