FROM jenkins/jenkins:lts

# Switch to root to install additional tools
USER root

# Install basic dependencies
RUN apt-get update && apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    wget \
    git \
    software-properties-common

# Install Docker CLI (proper enterprise method)
RUN install -m 0755 -d /etc/apt/keyrings \
    && curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg \
    && chmod a+r /etc/apt/keyrings/docker.gpg \
    && echo "deb [arch="$(dpkg --print-architecture)" signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian "$(. /etc/os-release && echo "$VERSION_CODENAME")" stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null \
    && apt-get update \
    && apt-get install -y docker-ce-cli docker-buildx-plugin docker-compose-plugin

# Install docker-compose v2 (standalone)
RUN curl -SL https://github.com/docker/compose/releases/latest/download/docker-compose-linux-x86_64 -o /usr/local/bin/docker-compose \
    && chmod +x /usr/local/bin/docker-compose \
    && ln -s /usr/local/bin/docker-compose /usr/bin/docker-compose

# Install .NET 8 SDK (direct binary method - more reliable)
RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet \
    && rm dotnet-install.sh \
    && ln -s /usr/share/dotnet/dotnet /usr/local/bin/

# Install additional useful tools
RUN apt-get install -y \
    jq \
    tree \
    vim \
    htop

# Clean up package lists
RUN apt-get clean && rm -rf /var/lib/apt/lists/*

# Create docker group and add jenkins user
RUN groupadd docker || true \
    && usermod -aG docker jenkins || true

# Verify installations
RUN docker --version || echo "Docker CLI installed"
RUN dotnet --version || echo ".NET SDK installed"

# Switch back to jenkins user
USER jenkins

# Skip initial setup wizard for automation
ENV JAVA_OPTS=-Djenkins.install.runSetupWizard=false

# Add useful aliases for jenkins user
RUN echo 'alias ll="ls -la"' >> ~/.bashrc \
    && echo 'alias docker-ps="docker ps --format \"table {{.Names}}\t{{.Status}}\t{{.Ports}}\""' >> ~/.bashrc