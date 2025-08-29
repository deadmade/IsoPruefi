import { Link } from "react-router-dom";
import logo from "../assets/isopruefi.png";

export default function Welcome() {
  return (
    <div className="flex min-h-screen w-full bg-[#f5cacd]">
      {/* Left column */}
      <div className="w-1/2 flex items-center justify-center">
        <img
          src={logo}
          alt="IsoPruefi"
          className="w-auto max-w-[90%] max-h-[90%]"
        />
      </div>

      {/* Right column */}
      <div className="w-1/2 flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-5xl font-extrabold text-[#d3546c] mb-8">
            Welcome to IsoPrüfi
          </h1>
          <div className="flex gap-5 justify-center">
            <Link
              to="/signin"
              className="px-8 py-3 text-lg rounded-xl bg-pink-600 text-white hover:bg-pink-800"
            >
              Sign In
            </Link>
            <Link
              to="/signup"
              className="px-8 py-3 text-lg rounded-xl border border-white text-pink-800 hover:bg-white"
            >
              Sign Up
            </Link>
          </div>
        </div>
      </div>
    </div>

  );
}
