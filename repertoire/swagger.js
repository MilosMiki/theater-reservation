// swagger.js
const swaggerDefinition = {
    openapi: '3.0.0',
    info: {
      title: 'ITA Plays API',
      version: '1.0.0',
      description: 'API for managing plays',
    },
    components: {
      securitySchemes: {
        BearerAuth: {
          type: 'http',
          scheme: 'bearer',
          bearerFormat: 'JWT',
        },
      },
      schemas: {
        Play: {
          type: 'object',
          properties: {
            id: {
              type: 'integer',
              example: 1,
            },
            title: {
              type: 'string',
              example: 'Hamlet',
            },
            duration: {
              type: 'integer',
              example: 120,
            },
            description: {
              type: 'string',
              example: 'A classic Shakespearean tragedy.',
            },
            cast: {
              type: 'string',
              example: 'John Doe, Jane Doe',
            },
          },
          required: ['title', 'duration', 'description', 'cast'],
        },
      },
    },
    tags: [
      {
        name: 'Plays',
        description: 'Operations about plays',
      },
    ],
    paths: {
      '/plays': {
        get: {
          tags: ['Plays'],
          summary: 'Get all plays',
          responses: {
            '200': {
              description: 'A list of plays',
              content: {
                'application/json': {
                  schema: {
                    type: 'array',
                    items: {
                      $ref: '#/components/schemas/Play',
                    },
                  },
                },
              },
            },
            '500': { description: 'Internal server error' },
          },
        },
        post: {
          tags: ['Plays'],
          summary: 'Create a new play',
          description: 'Allows an admin to create a new play',
          security: [{ BearerAuth: [] }],
          requestBody: {
            required: true,
            content: {
              'application/json': {
                schema: {
                  $ref: '#/components/schemas/Play',
                },
              },
            },
          },
          responses: {
            '201': {
              description: 'Successfully created a new play',
              content: {
                'application/json': {
                  schema: {
                    $ref: '#/components/schemas/Play',
                  },
                },
              },
            },
            '400': { description: 'Invalid input' },
            '500': { description: 'Internal server error' },
          },
        },
      },
      '/plays/{playId}': {
        get: {
          tags: ['Plays'],
          summary: 'Get a play by ID',
          parameters: [
            {
              in: 'path',
              name: 'playId',
              required: true,
              schema: {
                type: 'integer',
              },
            },
          ],
          responses: {
            '200': {
              description: 'The requested play',
              content: {
                'application/json': {
                  schema: {
                    $ref: '#/components/schemas/Play',
                  },
                },
              },
            },
            '404': { description: 'Play not found' },
            '500': { description: 'Internal server error' },
          },
        },
        put: {
          tags: ['Plays'],
          summary: 'Update an existing play',
          description: 'Allows an admin to update the details of a play',
          security: [{ BearerAuth: [] }],
          parameters: [
            {
              in: 'path',
              name: 'playId',
              required: true,
              schema: {
                type: 'integer',
              },
            },
          ],
          requestBody: {
            required: true,
            content: {
              'application/json': {
                schema: {
                  $ref: '#/components/schemas/Play',
                },
              },
            },
          },
          responses: {
            '200': {
              description: 'Successfully updated the play',
              content: {
                'application/json': {
                  schema: {
                    $ref: '#/components/schemas/Play',
                  },
                },
              },
            },
            '400': { description: 'Invalid input' },
            '404': { description: 'Play not found' },
            '500': { description: 'Internal server error' },
          },
        },
        delete: {
          tags: ['Plays'],
          summary: 'Delete a play',
          description: 'Allows an admin to delete a play by its ID',
          security: [{ BearerAuth: [] }],
          parameters: [
            {
              in: 'path',
              name: 'playId',
              required: true,
              schema: {
                type: 'integer',
              },
            },
          ],
          responses: {
            '204': { description: 'Successfully deleted the play' },
            '404': { description: 'Play not found' },
            '500': { description: 'Internal server error' },
          },
        },
      },
    },
  };
  
  module.exports = swaggerDefinition;