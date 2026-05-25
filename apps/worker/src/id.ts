export function createId(prefix: string): string {
  return `${prefix}_${crypto.randomUUID().replaceAll("-", "")}`;
}

export function assetKey(kind: string, id: string, extension: string): string {
  return `${kind}/${id}.${extension}`;
}
