FROM envoyproxy/envoy:866a4bab8c1e7809e26409ac9636843070b2f61c

RUN apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y \
    ca-certificates \
    software-properties-common \
    apt-transport-https \
    libssh2.1 \
    liblttng-ust0 \
    curl \
    iproute2 \
    net-tools

EXPOSE 19081
EXPOSE 8001
WORKDIR /app

COPY Third-Party-Notices.txt /
COPY . /app
RUN chmod 755 startup.sh
CMD ./startup.sh
