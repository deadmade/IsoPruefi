{
  description = "PGA-SE-KSTH dev shell";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-25.05";

  outputs = { self, nixpkgs, ... } @ inputs: let
    system = "x86_64-linux";
    pkgs = nixpkgs.legacyPackages.${system};
  in {
    devShells.${system}.default = pkgs.mkShell {
      packages = [
	pkgs.dotnet-sdk_9
	pkgs.nodejs
	pkgs.python313
	pkgs.python313Packages.pip
        pkgs.docker
        pkgs.lazydocker
      ];
            shellHook = ''
              npm install
            
              python -m venv .venv
              source .venv/bin/activate
              pip install -r documentation/requirements.txt

              cd frontend
              npm install
              cd ..
            '';
    };
  };
}
