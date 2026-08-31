export const environment = {
  production: true,

  bff: {
    baseUrl: 'https://localhost:7142',
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
