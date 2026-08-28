export interface LoginRequest { email: string; password: string; }
export interface RegisterRequest { firstName: string; lastName: string; email: string; password: string; }
export interface AuthenticatedUser { id: string; email: string; displayName: string; avatarUrl?: string | null; }
/** Frontend session metadata only: never place JWTs or integration credentials here. */
export interface Session { id?: string; user: AuthenticatedUser; }
export interface BffAuthResponse { sessionId?: string; user?: AuthenticatedUser; userId?: string; email?: string; displayName?: string; avatarUrl?: string | null; }
export interface ApiError { status?: number; message: string; fieldErrors?: Record<string, string[]>; }
