{
  description = "PGA-SE-KSTH dev shell";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-25.05";

  outputs = { self, nixpkgs, ... } @ inputs: let
    system = "x86_64-linux";

    # Provides a fake "docker" binary mapping to podman
    dockerCompat = pkgs.runCommandNoCC "docker-podman-compat" {} ''
    mkdir -p $out/bin
    ln -s ${pkgs.podman}/bin/podman $out/bin/docker
    '';

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

        dockerCompat
        pkgs.podman  # Docker compat
        pkgs.podman-compose
        pkgs.runc  # Container runtime
      ];

      shellHook = ''
        npm install
      
        python -m venv .venv
        source .venv/bin/activate
        pip install -r isopruefi-docs/requirements.txt

        cd frontend
        npm install

        export DOCKER_HOST="unix://$XDG_RUNTIME_DIR/podman/podman.sock"
        export DOCKER_SOCK="$XDG_RUNTIME_DIR/podman/podman.sock"
        podman system service --time=0 &
      '';
    };
  };
}
