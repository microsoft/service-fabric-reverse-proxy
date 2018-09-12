FROM envoyproxy/envoy:866a4bab8c1e7809e26409ac9636843070b2f61c

RUN apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y \
    ca-certificates \
    software-properties-common \
    apt-transport-https \
    libssh2.1 \
    curl \
    haveged \
    iproute2 \
    net-tools


# dotnet
RUN curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
RUN  mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
RUN sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-xenial-prod xenial main" > /etc/apt/sources.list.d/dotnetdev.list'
RUN apt-get update
RUN DEBIAN_FRONTEND=noninteractive apt-get install -y dotnet-sdk-2.1.105 aspnetcore-store-2.0.7

EXPOSE 19081
EXPOSE 8001
WORKDIR /app

COPY Third-Party-Notices.txt /
COPY . /app
RUN chmod 755 startup.sh
CMD ./startup.sh
