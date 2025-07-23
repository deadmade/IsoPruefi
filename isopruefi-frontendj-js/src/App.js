
function MyLink() {
    return (
        <a href={"https://react.dev/learn"}
           target="_blank"
           rel="noopener noreferrer"
    >This is my link</a>
    )
}

export default function App() {
    return (
    <div>
        <h1>Hello World! </h1>
        <MyLink />
    </div>
    )
}
