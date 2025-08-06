import {useState} from "react";
import { login, register } from "../utils/authApi.ts";

type AuthFormProps = {
    mode: "signin" | "signup";
    onSuccess?: (data: any) => void; // callback after success (optional) (chatgpt generated)
}

export default function AuthForm({mode, onSuccess}: AuthFormProps) {
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setLoading(true);

        try {
            let result;
            if (mode === "signin") {
                result = await login(username, password);
                console.log("Login success:", result);
                // Here we store tokens in localStorage
                localStorage.setItem("accessToken", result.token);
                localStorage.setItem("refreshToken", result.refreshToken);
            } else {
                result = await register(username, password);
                console.log("Registration success:", result);
            }

            if (onSuccess) onSuccess(result);
        } catch (err: any) {
            setError(err.message || "Something went wrong");
        } finally {
            setLoading(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", maxWidth: 300 }}>
            <h2>{mode === "signin" ? "Sign In" : "Sign Up"}</h2>

            <input
                type="text"
                placeholder="Username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                required
                style={{ marginBottom: 10, padding: 8 }}
            />

            <input
                type="password"
                placeholder="Password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                style={{ marginBottom: 10, padding: 8 }}
            />

            {error && <div style={{ color: "red", marginBottom: 10 }}>{error}</div>}

            <button type="submit" disabled={loading}>
                {loading ? "Please wait..." : mode === "signin" ? "Sign In" : "Sign Up"}
            </button>
        </form>
    );
}

