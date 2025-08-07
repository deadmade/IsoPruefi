import {useNavigate} from "react-router-dom";
import AuthForm from "./AuthForm.tsx";

export default function SignIn() {

    const navigate = useNavigate();

    const handleSuccess = (data: any) => {
        console.log("Signed in successfully:", data);
        navigate("/user");
    }

    const style = { padding: 20 };
    return (
        <div style={style}>
            <AuthForm mode="signin" onSuccess={handleSuccess} />
        </div>
    );
}