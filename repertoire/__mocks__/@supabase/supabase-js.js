module.exports = {
    createClient: jest.fn().mockImplementation(() => ({
      from: jest.fn(() => ({
        select: jest.fn(),
        insert: jest.fn(),
        update: jest.fn(),
        delete: jest.fn(),
        eq: jest.fn(),
        single: jest.fn()
      }))
    }))
  };