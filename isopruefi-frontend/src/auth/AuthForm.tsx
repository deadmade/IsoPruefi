import { useState } from "react";
import { login, register } from "../utils/authApi.ts";
import {decodeToken, type JwtPayload, saveToken} from "../utils/tokenHelpers";
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

                // store tokens
                saveToken(tokenData.token, tokenData.refreshToken);

                // decode and normalize roles
                const decoded: JwtPayload = decodeToken(tokenData.token) ?? {};
                const claim =
                    decoded.role ??
                    decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

                const roles: string[] =
                    Array.isArray(claim) ? claim :
                        typeof claim === "string" && claim ? [claim] : [];

                // route based on role
                if (roles.includes("Admin")) {
                    navigate("/admin");
                } else if (roles.includes("User")) {
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
  <form
    onSubmit={handleSubmit}
    className="flex flex-col gap-4 max-w-sm mx-auto"
  >
    <h2 className="text-3xl font-bold text-center text-[#d3546c] mb-4">
      {mode === "signin" ? "Sign In" : "Sign Up"}
    </h2>

    <label>
      <div>
        <span className="block text-sm font-bold text-gray-700 mb-1">
          Username
        </span>
        <input
          type="text"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          required
          className="w-full rounded-lg border border-gray-300 px-4 py-2
                     focus:outline-none focus:ring-2 focus:ring-pink-300 focus:border-pink-300"
        />
      </div>
    </label>

    <label>
      <div>
        <span className="block text-sm font-bold text-gray-700 mb-1">
          Password
        </span>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          className="w-full rounded-lg border border-gray-300 px-4 py-2
                     focus:outline-none focus:ring-2 focus:ring-pink-300 focus:border-pink-300"
        />
      </div>
    </label>

    <button
      type="submit"
      className="w-full mt-2 rounded-lg bg-pink-600 text-white py-2 font-semibold hover:bg-pink-800"
    >
      {mode === "signin" ? "Login" : "Register"}
    </button>

    {error && <p className="text-red-600 text-sm mt-2 text-center">{error}</p>}
  </form>
);
}
