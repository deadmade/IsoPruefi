import {useNavigate} from "react-router-dom";
import AuthForm from "./AuthForm.tsx";

export default function SignUp() {

    const navigate = useNavigate();

    const handleSuccess = (data: any) => {
        console.log("Signed up successfully:", data);
        navigate("/signin");
    }

    const style = {padding: 20};

    return (
        <div style={style}>
            <AuthForm mode={"signup"} onSuccess={handleSuccess}/>
        </div>
    )
}