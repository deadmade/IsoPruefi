import {BrowserRouter as Router, Routes, Route} from "react-router-dom";
import Welcome from "./pages/Welcome.tsx";
import SignIn from "./auth/SignIn.tsx";
import SignUp from "./auth/SignUp.tsx";
import UserPage from "./pages/UserPage.tsx";
import AdminPage from "./pages/AdminPage.tsx";
// import {WeatherChartTitle} from "./components/Weather.tsx"
// import {TempChart} from "./components/Weather.tsx";

// entry point of the website. shows WeatherChartTitle and TempChart

export default function App() {

    return (
        <Router>
            <Routes>
                <Route path="/" element={<Welcome />} />
                <Route path="/signin" element={<SignIn />} />
                <Route path="/signup" element={<SignUp />} />
                <Route path="/user" element={<UserPage />} />
                <Route path="/admin" element={<AdminPage />} />
            </Routes>
        </Router>
    );
}
