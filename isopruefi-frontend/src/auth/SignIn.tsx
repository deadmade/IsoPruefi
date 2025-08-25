import AuthForm from "./AuthForm.tsx";

export default function SignIn() {
    
    const style = { padding: 20 };
    return (
        <div style={style}>
            <AuthForm mode="signin"/>
        </div>
    );
}