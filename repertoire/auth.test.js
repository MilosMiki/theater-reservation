const jwt = require('jsonwebtoken');
const { authenticateAdmin } = require('./server');

jest.mock('jsonwebtoken', () => ({
  sign: jest.fn(),
  verify: jest.fn()
}));

describe('Authentication Middleware', () => {
  let req, res, next;

  beforeEach(() => {
    req = {
      headers: {}
    };
    res = {
      status: jest.fn().mockReturnThis(),
      json: jest.fn()
    };
    next = jest.fn();
  });

  it('should reject requests without authorization header', () => {
    authenticateAdmin(req, res, next);
    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalledWith({ error: "Unauthorized" });
  });

  it('should reject requests with invalid token', () => {
    req.headers.authorization = 'Bearer invalidtoken';
    jwt.verify.mockImplementation(() => {
      throw new Error('Invalid token');
    });
    authenticateAdmin(req, res, next);
    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalledWith({ error: "Invalid token" });
  });

  it('should reject non-admin users', () => {
    jwt.verify.mockReturnValue({ role: 'user' });
    req.headers.authorization = 'Bearer validtoken';
    authenticateAdmin(req, res, next);
    expect(res.status).toHaveBeenCalledWith(403);
    expect(res.json).toHaveBeenCalledWith({ error: "Forbidden: Admins only" });
  });

  it('should allow admin users', () => {
    jwt.verify.mockReturnValue({ role: 'admin' });
    req.headers.authorization = 'Bearer validtoken';
    authenticateAdmin(req, res, next);
    expect(next).toHaveBeenCalled();
    expect(req.user).toEqual({ role: 'admin' });
  });
});