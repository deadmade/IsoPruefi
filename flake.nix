{
  description = "PGA-SE-KSTH dev shell";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-25.05";

  outputs = { self, nixpkgs, ... } @ inputs: let
    system = "x86_64-linux";

    pkgs = import nixpkgs {system = system; config.allowUnfree = true;};
  in {
    devShells.${system}.default = pkgs.mkShell {
      packages = [
	pkgs.dotnet-sdk_9
	pkgs.nodejs
	pkgs.python313
	pkgs.python313Packages.pip
        pkgs.docker
        pkgs.lazydocker
        pkgs.python313Packages.weasyprint

        pkgs.arduino-ide
        pkgs.mqtt-explorer

        pkgs.act
      ];

      shellHook = ''
        npm install
      
        python -m venv .venv
        source .venv/bin/activate
        pip install -r isopruefi-docs/requirements.txt

        cd isopruefi-frontend
        npm install
        cd ..
      '';
    };
  };
}
