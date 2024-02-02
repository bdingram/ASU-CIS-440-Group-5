
const express = require('express');
const bodyParser = require('body-parser');
const mysql = require('mysql');

const app = express();
app.use(bodyParser.json());

// Use getConString for the database connection
const dbConfig = getConString();

// Configure your SQL database connection
const db = mysql.createConnection(dbConfig);

// Connect to the database
db.connect((err) => {
    if (err) {
        throw err;
    }
    console.log('Connected to the database');
});

// Login endpoint
app.post('/login', (req, res) => {
    const { username, password } = req.body;

    // SQL query to check for user
    // Use parameterized queries to prevent SQL injection
    const query = 'SELECT * FROM users WHERE username = ? AND password = ?';
    db.query(query, [username, password], (err, result) => {
        
    });
    if (/* condition for successful login */) {
        res.json({ success: true });
    } else {
        res.json({ success: false });
    }
});

// Start the server
const PORT = 3000;
app.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
});

