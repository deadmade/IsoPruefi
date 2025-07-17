# Contribute & Build 

## How do I contribute as a developer?
<p style="color: red;"><b>READ THIS GUIDE BEFORE CONTRIBUTING</b></p>

Since our project is secured by two pre-commit hocks, it is important to set up the project correctly before contributing.

This is done as followed:

Clone the project

```git clone https://github.com/deadmade/PGA-SE-KSTH.git```

Make sure you have installed the following packages globally.

- <a href="https://www.python.org/">Python</a>: Needed for MkDocs
- <a href="https://www.npmjs.com/">Node Package Manager</a>: Used to install needed dependencies for pre-commit hooks
- <a href="https://dotnet.microsoft.com/en-us/download">.NET 9.0 SDK</a>: Used for our Rest-API
- <a href="https://www.docker.com/">Docker</a>

After you've cloned the repo make sure to install all needed packages for the hooks via:

```npm i```

and run:

```npm run init```

Now it should be configured 🚀

To be able to start coding, run 

```develop compose up```

in the IsoPrüfi Directory

Happy Coding 😊
