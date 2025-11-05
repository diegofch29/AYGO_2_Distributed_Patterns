import "./App.css";
import { useState, useEffect } from "react";
import { getApiEndpoint } from "./config";

interface LogRecord {
  name: string;
  timestamp: string;
}

function App() {
  const [records, setRecords] = useState<LogRecord[]>([]);
  const [loading, setLoading] = useState(false);

  const fetchRecords = async () => {
    setLoading(true);
    try {
      const response = await fetch(getApiEndpoint("/api/log"), {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (response.ok) {
        const data = await response.json();
        setRecords(data);
        console.log("Records fetched successfully");
      } else {
        console.error("Failed to fetch records:", response.status);
      }
    } catch (error) {
      console.error("Error fetching records:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRecords();
  }, []);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault(); // Prevent default form submission

    const formData = new FormData(event.currentTarget);
    const name = formData.get("name") as string;

    try {
      const response = await fetch(getApiEndpoint("/api/log"), {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ name }),
      });

      if (response.ok) {
        console.log("Request sent successfully");
        // Refresh the records list after successful submission
        fetchRecords();
        // Optionally clear the form
        event.currentTarget.reset();
      } else {
        console.error("Request failed:", response.status);
      }
    } catch (error) {
      console.error("Error sending request:", error);
    }
  };

  return (
    <div>
      <form onSubmit={handleSubmit}>
        <label htmlFor="name">
          Name:
          <input type="text" name="name" id="name" />
        </label>
        <button type="submit">Send</button>
      </form>

      <div style={{ marginTop: "20px" }}>
        <h2>Log Records</h2>
        <button onClick={fetchRecords} disabled={loading}>
          {loading ? "Loading..." : "Refresh"}
        </button>

        {loading ? (
          <p>Loading records...</p>
        ) : (
          <div style={{ marginTop: "10px" }}>
            {records.length === 0 ? (
              <p>No records found.</p>
            ) : (
              <table style={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                  <tr>
                    <th style={{ border: "1px solid #ccc", padding: "8px" }}>
                      Name
                    </th>
                    <th style={{ border: "1px solid #ccc", padding: "8px" }}>
                      Timestamp
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {records
                    .sort(
                      (a, b) =>
                        new Date(b.timestamp).getTime() -
                        new Date(a.timestamp).getTime()
                    )
                    .slice(0, 10)
                    .map((record, index) => (
                      <tr key={index}>
                        <td
                          style={{ border: "1px solid #ccc", padding: "8px" }}
                        >
                          {record.name}
                        </td>
                        <td
                          style={{ border: "1px solid #ccc", padding: "8px" }}
                        >
                          {new Date(record.timestamp).toLocaleString()}
                        </td>
                      </tr>
                    ))}
                </tbody>
              </table>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default App;
