FROM envoyproxy/envoy:latest

RUN apt-get update
RUN DEBIAN_FRONTEND=noninteractive apt-get install -y \
    ca-certificates \
    software-properties-common \
    apt-transport-https \
    libssh2.1 \
    curl

# dotnet
RUN curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
RUN  mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
RUN sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-xenial-prod xenial main" > /etc/apt/sources.list.d/dotnetdev.list'
RUN apt-get update
RUN DEBIAN_FRONTEND=noninteractive apt-get install -y dotnet-sdk-2.1.4

EXPOSE 19081
EXPOSE 8001
WORKDIR /app

COPY . /app
RUN chmod 755 startup.sh
CMD ./startup.sh
