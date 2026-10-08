import { environment } from '../../../environments/environment';

/**
 * Resolves a possibly-relative media path (as returned by the BFF, e.g.
 * "/uploads/x.png") into an absolute URL against the BFF base URL.
 * Already-absolute URLs (http/https) are returned unchanged.
 */
export function resolveMediaUrl(path?: string | null): string {
  if (!path) return '';
  if (/^https?:\/\//i.test(path)) return path;
  const base = environment.bff.baseUrl.replace(/\/$/, '');
  return `${base}/${path.replace(/^\//, '')}`;
}
