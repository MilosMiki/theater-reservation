const request = require('supertest');
const { app, supabase } = require('./server');
const jwt = require('jsonwebtoken');

jest.mock('@supabase/supabase-js');

describe('Plays API', () => {
  let adminToken;
  let regularToken;

  beforeAll(() => {
    adminToken = jwt.sign({ role: 'admin' }, process.env.JWT_SECRET);
    regularToken = jwt.sign({ role: 'user' }, process.env.JWT_SECRET);
    jest.spyOn(console, 'log').mockImplementation(() => {});
    jest.spyOn(console, 'error').mockImplementation(() => {});
    jest.spyOn(console, 'warn').mockImplementation(() => {});
  });

  describe('GET /plays', () => {
    it('should return all plays', async () => {
      const mockPlays = [{ id: 1, title: 'Test Play' }];
      supabase.from.mockReturnValue({
        select: jest.fn().mockReturnValue({ data: mockPlays, error: null })
      });

      const res = await request(app).get('/plays');
      expect(res.status).toBe(200);
      expect(res.body).toEqual(mockPlays);
    });

    it('should handle errors', async () => {
      supabase.from.mockReturnValue({
        select: jest.fn().mockReturnValue({ data: null, error: { message: 'Database error' } })
      });

      const res = await request(app).get('/plays');
      expect(res.status).toBe(500);
      expect(res.body.error).toBe('Database error');
    });
  });

  describe('GET /plays/:playId', () => {
    it('should return a single play', async () => {
      const mockPlay = { id: 1, title: 'Test Play' };
      supabase.from.mockReturnValue({
        select: jest.fn().mockReturnThis(),
        eq: jest.fn().mockReturnThis(),
        single: jest.fn().mockReturnValue({ data: mockPlay, error: null })
      });
  
      const res = await request(app).get('/plays/1');
      expect(res.status).toBe(200);
      expect(res.body).toEqual(mockPlay);
    });
  
    it('should return 404 if play not found', async () => {
        supabase.from.mockReturnValue({
          select: jest.fn().mockReturnThis(),
          eq: jest.fn().mockReturnThis(),
          single: jest.fn().mockReturnValue({ 
            data: null,
            error: { message: 'JSON object requested, multiple (or no) rows returned' }
          })
        });
    
        const res = await request(app).get('/plays/999');
        expect(res.status).toBe(404);
        expect(res.body.error).toBe('Play not found');
    });

    it('should return 500 if there is a real server error', async () => {
    supabase.from.mockReturnValue({
        select: jest.fn().mockReturnThis(),
        eq: jest.fn().mockReturnThis(),
        single: jest.fn().mockReturnValue({ 
          data: null,
          error: { message: 'Database connection failed' }
        })
    });

    const res = await request(app).get('/plays/999');
    expect(res.status).toBe(500);
    expect(res.body.error).toBe('Database connection failed');
    });
  });

  describe('POST /plays', () => {
    it('should create a new play (admin)', async () => {
      const newPlay = { title: 'New Play', duration: 120 };
      const mockResponse = { ...newPlay, id: 2 };
      
      supabase.from.mockReturnValue({
        insert: jest.fn().mockReturnValue({ data: [mockResponse], error: null })
      });

      const res = await request(app)
        .post('/plays')
        .set('Authorization', `Bearer ${adminToken}`)
        .send(newPlay);

      expect(res.status).toBe(201);
      expect(res.body).toEqual([mockResponse]);
    });

    it('should reject unauthorized users', async () => {
      const res = await request(app)
        .post('/plays')
        .send({ title: 'New Play' });

      expect(res.status).toBe(401);
    });

    it('should reject non-admin users', async () => {
      const res = await request(app)
        .post('/plays')
        .set('Authorization', `Bearer ${regularToken}`)
        .send({ title: 'New Play' });

      expect(res.status).toBe(403);
    });
  });

  describe('PUT /plays/:playId', () => {
    it('should update a play (admin)', async () => {
      const updatedPlay = { id:1, title: 'Updated Play' };
      const mockResponse = { ...updatedPlay, id: 1 };
      
      supabase.from.mockReturnValue({
        update: jest.fn().mockReturnThis(),
        select: jest.fn().mockReturnThis(),
        eq: jest.fn().mockReturnThis(),
        single: jest.fn().mockReturnValue({ data: updatedPlay, error: null })
      });

      const res = await request(app)
        .put('/plays/1')
        .set('Authorization', `Bearer ${adminToken}`)
        .send(updatedPlay);

      expect(res.status).toBe(200);
      expect(res.body).toEqual(mockResponse);
    });
    it('should return 404 when play is not found', async () => {
      const updatedPlay = { id:1, title: 'Updated Play' };
      
      supabase.from.mockReturnValue({
        update: jest.fn().mockReturnThis(),
        select: jest.fn().mockReturnThis(),
        eq: jest.fn().mockReturnThis(),
        single: jest.fn().mockReturnValue({ 
          data: null,
          error: { message: 'JSON object requested, multiple (or no) rows returned' }
        })
      });

      const res = await request(app)
        .put('/plays/999')
        .set('Authorization', `Bearer ${adminToken}`)
        .send(updatedPlay);

      expect(res.status).toBe(404);
      expect(res.body).toEqual({ error: "Play not found" });
    });
    it('should return 500 when database error occurs', async () => {
      const updatedPlay = { id:1, title: 'Updated Play' };
      const mockError = new Error('Database error');
      
      supabase.from.mockReturnValue({
        update: jest.fn().mockReturnThis(),
        select: jest.fn().mockReturnThis(),
        eq: jest.fn().mockReturnThis(),
        single: jest.fn().mockReturnValue({ 
          data: null,
          error: mockError
        })
      });
    
      const res = await request(app)
        .put('/plays/1')
        .set('Authorization', `Bearer ${adminToken}`)
        .send(updatedPlay);
    
      expect(res.status).toBe(500);
      expect(res.body).toEqual({ error: mockError.message });
    });
  });

  describe('DELETE /plays/:playId', () => {
    it('should delete a play (admin)', async () => {
      supabase.from.mockReturnValue({
        delete: jest.fn().mockReturnThis(),
        eq: jest.fn().mockReturnValue({ 
          error: null, 
          count: 1,
          data: null
        })
      });

      const res = await request(app)
        .delete('/plays/1')
        .set('Authorization', `Bearer ${adminToken}`);

      expect(res.status).toBe(204);
    });
  });

  it('should return 404 when play is not found', async () => {
    supabase.from.mockReturnValue({
      delete: jest.fn().mockReturnThis(),
      eq: jest.fn().mockReturnValue({ 
        error: null, 
        count: 0,
        data: null
      })
    });

    const res = await request(app)
      .delete('/plays/999')
      .set('Authorization', `Bearer ${adminToken}`);

    expect(res.status).toBe(404);
    expect(res.body).toEqual({ error: "Play not found" });
  });

  it('should return 500 when database error occurs', async () => {
    const mockError = new Error('Database connection failed');
    
    supabase.from.mockReturnValue({
      delete: jest.fn().mockReturnThis(),
      eq: jest.fn().mockReturnValue({ 
        error: mockError, 
        count: null,
        data: null
      })
    });

    const res = await request(app)
      .delete('/plays/1')
      .set('Authorization', `Bearer ${adminToken}`);

    expect(res.status).toBe(500);
    expect(res.body).toEqual({ error: mockError.message });
  });
});