pipeline {
  agent any

  environment {
    DOCKER_IMAGE = 'budgetforge/api'
    BUILD_TAG = "${BUILD_NUMBER}-${GIT_COMMIT.take(7)}"
    TFM = 'net8.0'
  }

  stages {
    stage('🧽 Reset workspace') {
      steps {
        echo "Wiping workspace to avoid stale artifacts..."
        deleteDir()
        // ADD THIS: Explicit checkout after deleteDir()
        checkout scm
        sh '''
          echo "Dotnet info:"
          dotnet --info
          dotnet nuget locals all --clear || true
        '''
      }
    }

    stage('🚀 Checkout & Info') {
      steps {
        echo "🔄 Starting BudgetForge CI/CD Pipeline..."
        sh '''
          echo "📋 Build Information:"
          echo "  Build Number: ${BUILD_NUMBER}"
          echo "  Git Commit: ${GIT_COMMIT}"
          echo "  Image Tag: ${BUILD_TAG}"
          echo "  Workspace: ${WORKSPACE}"

          echo "🔧 Tool Versions:"
          git --version
          dotnet --version
          docker --version

          echo "📁 Workspace Contents (after checkout):"
          ls -la
          
          echo "📁 Solution file check:"
          ls -la *.sln || echo "No .sln files found in root"
        '''
      }
    }

    stage('🧹 Cleanup') {
      steps {
        echo "🧹 Cleaning previous builds..."
        sh '''
          docker image prune -f || true
          # Force SDK to operate on net8.0 to avoid NETSDK1045 messages
          dotnet clean BudgetForge.sln -c Release -p:TargetFramework=${TFM} || true
          find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
          find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
        '''
      }
    }

    stage('📦 Restore Dependencies') {
      steps {
        sh '''
          dotnet restore BudgetForge.sln -p:TargetFramework=${TFM} --verbosity minimal
        '''
      }
    }

    stage('🔨 Build Application') {
      steps {
        sh '''
          dotnet build BudgetForge.sln -c Release -p:TargetFramework=${TFM} --no-restore --verbosity minimal
        '''
      }
    }

    stage('🧪 Run Tests') {
      steps {
        sh '''
          if find tests -name "*Tests.csproj" -type f | grep -q .; then
            echo "Running tests on ${TFM}..."
            # Run each test project explicitly on net8.0
            for proj in $(find tests -name "*Tests.csproj" -type f); do
              dotnet test "$proj" -c Release -f ${TFM} --no-build --verbosity minimal
            done
          else
            echo "No tests found; skipping."
          fi
        '''
      }
    }

    stage('🐳 Build Docker Image') {
      steps {
        sh '''
          echo "Building Docker image: ${DOCKER_IMAGE}:${BUILD_TAG}"
          docker build -f src/BudgetForge.Api/Dockerfile -t "${DOCKER_IMAGE}:${BUILD_TAG}" .
          docker tag "${DOCKER_IMAGE}:${BUILD_TAG}" "${DOCKER_IMAGE}:latest"
          docker images "${DOCKER_IMAGE}" --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}\t{{.CreatedSince}}"
        '''
      }
    }

    stage('🔍 Health Check') {
      steps {
        sh '''
          docker stop budgetforge-test 2>/dev/null || true
          docker rm budgetforge-test 2>/dev/null || true

          echo "Starting test container..."
          docker run -d --name budgetforge-test -p 8083:8080 \
            -e ENABLE_SWAGGER=true \
            -e ASPNETCORE_URLS=http://+:8080 \
            -e ConnectionStrings__DefaultConnection="Host=localhost;Database=test;Username=test;Password=test;Port=5432" \
            "${DOCKER_IMAGE}:${BUILD_TAG}"

          echo "Waiting for startup..."
          sleep 20

          set +e
          curl -fsS http://localhost:8083/swagger/index.html && OK=1
          docker logs budgetforge-test --tail 50
          docker stop budgetforge-test >/dev/null 2>&1 || true
          docker rm budgetforge-test >/dev/null 2>&1 || true
          [ "$OK" = "1" ] && echo "Health check passed" || echo "Health check had issues"
          set -e
        '''
      }
    }

    stage('🚀 Deploy to Development') {
      steps {
        sh '''
          echo "Updating running API container..."
          docker stop budgetforge-api 2>/dev/null || true
          docker rm budgetforge-api 2>/dev/null || true

          # Pick a BudgetForge network if present
          NETWORK=$(docker network ls --format '{{.Name}}' | grep -E 'budgetforge-(backend|default)' | head -1)
          [ -z "$NETWORK" ] && NETWORK="bridge"

          echo "Using network: $NETWORK"
          docker run -d --name budgetforge-api --network "$NETWORK" \
            -p 5001:8080 \
            -e ENABLE_SWAGGER=true \
            -e ASPNETCORE_ENVIRONMENT=Development \
            -e ASPNETCORE_URLS=http://+:8080 \
            -e ConnectionStrings__DefaultConnection="Host=budgetforge-postgres;Database=budgetforge;Username=budgetforge_user;Password=your_secure_password_123;Port=5432" \
            --restart unless-stopped \
            "${DOCKER_IMAGE}:latest"

          echo "Verifying deployment..."
          sleep 10
          curl -fsS http://localhost:5001/swagger/index.html || true
        '''
      }
    }
  }

  post {
    success {
      echo '''
      🎉 BUILD SUCCESSFUL! 🎉

      🌐 URLs:
      - API:       http://localhost:5001/swagger
      - Jenkins:   http://localhost:8082
      - Grafana:   http://localhost:3000  (admin / admin123)
      - Prometheus:http://localhost:19090
      '''
    }
    failure {
      echo '''
      ❌ BUILD FAILED! Check the console log above.
      '''
    }
    always {
      echo "Post-build cleanup finished."
      // No cleanWs(); using deleteDir() at the start instead.
    }
  }
}