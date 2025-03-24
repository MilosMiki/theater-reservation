const path = require("path");
const express = require("express");
const jwt = require("jsonwebtoken");

require("dotenv").config({ path: path.resolve(__dirname, "..", ".env") });
const { createClient } = require("@supabase/supabase-js");
const { create } = require("domain");

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

app.get("/plays", async (req, res) => {
    const { data, error } = await supabase.from("ita_plays").select("*");
    if (error) return res.status(500).json({ error: error.message });
    res.json(data);
});

app.get("/plays/:playId", async (req, res) => {
    const { playId } = req.params;
    const { data, error } = await supabase.from("ita_plays").select("*").eq("id", playId).single();
    if (error) return res.status(500).json({ error: error.message });
    res.json(data);
});

app.post("/plays", authenticateAdmin, async (req, res) => {
    const { title, duration, description, cast } = req.body;
    const { data, error } = await supabase.from("ita_plays").insert([{ title, duration, description, cast }]);
    if (error) return res.status(500).json({ error: error.message });
    res.status(201).json(data);
});

app.put("/plays/:playId", authenticateAdmin, async (req, res) => {
    const { playId } = req.params;
    const { title, duration, description, cast } = req.body;
    const { data, error } = await supabase.from("ita_plays").update({ title, duration, description, cast }).eq("id", playId);
    if (error) return res.status(500).json({ error: error.message });
    res.json(data);
});

app.delete("/plays/:playId", authenticateAdmin, async (req, res) => {
    const { playId } = req.params;
    const { error } = await supabase.from("ita_plays").delete().eq("id", playId);
    if (error) return res.status(500).json({ error: error.message });
    res.status(204).send();
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`Server running on port ${PORT}`));
