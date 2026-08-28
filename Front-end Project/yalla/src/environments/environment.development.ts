export const environment = {
  production: false,

  bff: {
    baseUrl: 'https://localhost:7142',

    // We use an HttpOnly cookie for the session.
    // Angular must not manage or send a session ID manually.
    sessionHeaderName: '',
    useCookieSession: true,

    auth: {
      loginPath: '/auth/login',
      registerPath: '/auth/register',
      logoutPath: '/auth/logout',
      googlePath: '/auth/google',
    },
  },
};
