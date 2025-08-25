import { Link } from 'react-router-dom';

export default function Welcome() {

    const style = {padding: 20};
    return (
        <div style={style}>
            <h1>Welcome to IsoPruefi</h1>

            <p>
                <Link to={"/signin"}>Sign In</Link>
            </p>
            <p>
                <Link to={"/signup"}>Sign Up</Link>
            </p>
        </div>
    );
}
