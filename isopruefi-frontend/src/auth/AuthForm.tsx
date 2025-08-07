import {useState} from "react";
import { login, register } from "../utils/authApi.ts";

type Mode = "signin" | "signup";

interface AuthFormProps {
    mode: Mode;
    onSuccess: (user: any) => void;
}

export default function AuthForm({ mode, onSuccess }: AuthFormProps) {
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        try {
            if (mode === "signin") {
                const tokenData = await login(username, password);

                // Save token and refreshToken in localStorage
                localStorage.setItem("token", tokenData.token);
                localStorage.setItem("token", tokenData.accessToken);
                localStorage.setItem("refreshToken", tokenData.refreshToken);

                onSuccess(tokenData);
            } else {
                await register(username, password);
                alert("Registration successful. You can now log in.");
                onSuccess("/signin");
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
