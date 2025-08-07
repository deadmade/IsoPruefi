import { useState } from "react";
import { login, register } from "../utils/authApi.ts";
import {decodeToken, saveToken} from "../utils/tokenHelpers";
import { useNavigate } from "react-router-dom";

type Mode = "signin" | "signup";

interface AuthFormProps {
    mode: Mode;
}

export default function AuthForm({ mode }: AuthFormProps) {
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);

    const navigate = useNavigate();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        try {
            if (mode === "signin") {
                const tokenData = await login(username, password);
                saveToken(tokenData.token, tokenData.refreshToken);
                const decoded = decodeToken(tokenData.token);
                console.log("Decoded JWT:", decoded);

                if (decoded?.role === "Admin") {
                    navigate("/admin");
                } else if (decoded?.role === "User") {
                    navigate("/user");
                } else {
                    navigate("/");
                }
            } else {
                await register(username, password);
                alert("Registration successful. You can now log in.");
                navigate("/signin");
            }
        } catch (err: any) {
            setError(err.message || "Something went wrong.");
        }
    };

    return (
        <form onSubmit={handleSubmit} style={{ maxWidth: 300, margin: "auto" }}>
            <h2>{mode === "signin" ? "Sign In" : "Sign Up"}</h2>

            <label>
                <div>
                    Username:
                    <p>
                        <input
                            type="text"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            required
                        />
                    </p>
                </div>
            </label>

            <label>
                <div>
                    Password:  
                    <p>
                        <input
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                        />
                    </p>
                </div>
            </label>

            <button type="submit" style={{ marginTop: 12 }}>
                {mode === "signin" ? "Login" : "Register"}
            </button>

            {error && <p style={{ color: "red" }}>{error}</p>}
        </form>
    );
}
