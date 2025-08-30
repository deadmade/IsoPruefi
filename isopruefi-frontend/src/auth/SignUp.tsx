import AuthForm from "./AuthForm.tsx";

export default function SignUp() {
    return (
        <div className="flex h-screen w-screen items-center justify-center bg-[#f5cacd]">
            <div className="w-full max-w-md bg-white rounded-xl shadow-lg p-8">
                <AuthForm mode="signup"/>
            </div>
        </div>
    );
}