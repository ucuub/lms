FROM gitpod/workspace-full:latest

# Install .NET 8 SDK
RUN sudo apt-get update && \
    sudo apt-get install -y wget apt-transport-https && \
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    sudo dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    sudo apt-get update && \
    sudo apt-get install -y dotnet-sdk-8.0 && \
    sudo apt-get clean

# Install EF Core tools globally
RUN dotnet tool install --global dotnet-ef && \
    echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
