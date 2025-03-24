const path = require("path");
const express = require("express");
const jwt = require("jsonwebtoken");
const swaggerJsdoc = require("swagger-jsdoc");
const swaggerUi = require("swagger-ui-express");
const swaggerDefinition = require('./swagger');

require("dotenv").config({ path: path.resolve(__dirname, "..", ".env") });
const { createClient } = require("@supabase/supabase-js");

const app = express();
app.use(express.json());

const supabase = createClient(process.env.SUPABASE_URL, process.env.SUPABASE_KEY);

const authenticateAdmin = (req, res, next) => {
    const authHeader = req.headers.authorization;
    if (!authHeader) return res.status(401).json({ error: "Unauthorized" });

    const token = authHeader.split(" ")[1];
    try {
        const decoded = jwt.verify(token, process.env.JWT_SECRET);
        if (decoded.role !== "admin") {
            return res.status(403).json({ error: "Forbidden: Admins only" });
        }
        req.user = decoded;
        next();
    } catch (error) {
        return res.status(401).json({ error: "Invalid token" });
    }
};

const swaggerSpec = swaggerJsdoc({
    swaggerDefinition,
    apis: [],
});

app.use('/api-docs', swaggerUi.serve, swaggerUi.setup(swaggerSpec));

app.use((req, res, next) => {
    console.log(`[${new Date().toISOString()}] ${req.method} ${req.url}`);
    next();
});

app.get("/plays", async (req, res) => {
    const { data, error } = await supabase.from("ita_plays").select("*");
    if (error) {
        console.error('Error:', error);
        return res.status(500).json({ error: error.message });
    }
    res.json(data);
});

app.get("/plays/:playId", async (req, res) => {
    const { playId } = req.params;
    const { data, error } = await supabase
    .from("ita_plays")
    .select("*")
    .eq("id", playId)
    .single();
    
    if (error) {
        if (error.message.includes('multiple (or no) rows returned')) {
        console.log(`Play not found with ID: ${playId}`);
        return res.status(404).json({ error: "Play not found" });
        }
        console.error('Database error:', error);
        return res.status(500).json({ error: error.message });
    }

    res.json(data);
});

app.post("/plays", authenticateAdmin, async (req, res) => {
    const { title, duration, description, cast } = req.body;
    const { data, error } = await supabase.from("ita_plays").insert([{ title, duration, description, cast }]);
    if (error) {
        console.error('Error:', error);
        return res.status(500).json({ error: error.message });
    }
    res.status(201).json(data);
});

app.put("/plays/:playId", authenticateAdmin, async (req, res) => {
    const { playId } = req.params;
    const { title, duration, description, cast } = req.body;
    const { data, error } = await supabase
        .from("ita_plays")
        .update({ title, duration, description, cast })
        .select("*")
        .eq("id", playId)
        .single();

    
    if (error) {
        if (error.message.includes('multiple (or no) rows returned')) {
        console.log(`Play not found with ID: ${playId}`);
        return res.status(404).json({ error: "Play not found" });
        }
        console.error('Database error:', error);
        return res.status(500).json({ error: error.message });
    }
    
    res.json(data);
});

app.delete("/plays/:playId", authenticateAdmin, async (req, res) => {
    const { playId } = req.params;
    
    const { data, error, count } = await supabase
        .from("ita_plays")
        .delete({ count: 'exact' })
        .eq("id", playId);
    
    if (error) {
        console.error('Error:', error);
        return res.status(500).json({ error: error.message });
    }
    
    if (count === 0) {
        console.log(`[${new Date().toISOString()}] Play not found with ID: ${playId}`);
        return res.status(404).json({ error: "Play not found" });
    }
    
    res.status(204).send();
});

const PORT = process.env.PORT || 3000;
let server;

if (process.env.NODE_ENV !== 'test') {
  server = app.listen(PORT, () => {
    console.log(`Server running on port ${PORT}, Swagger docs available at /api-docs`);
  });
}

module.exports = { app, supabase, server, authenticateAdmin};