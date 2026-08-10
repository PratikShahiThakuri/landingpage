pipeline {
    agent any

    environment {
        // Define your Docker image name
        IMAGE_NAME = 'landingmvc'
        // Define your registry (e.g., Docker Hub, AWS ECR, Azure ACR)
        DOCKER_REGISTRY = 'your-docker-registry.com'
    }

    stages {
        stage('Checkout') {
            steps {
                // Checkout code from source control
                checkout scm
            }
        }

        stage('Restore & Build .NET Core Application') {
            steps {
                echo 'Restoring NuGet packages and building the application...'
                // Using the .NET CLI installed on the Jenkins agent
                sh 'dotnet build landingmvc.csproj -c Release'
            }
        }

        stage('Run Tests') {
            steps {
                echo 'Running unit tests...'
                // Assuming there is a tests folder, otherwise this can be adjusted
                // sh 'dotnet test tests/landingmvc.Tests/landingmvc.Tests.csproj -c Release --no-build'
                echo 'Skipping tests - uncomment the line above when tests are configured.'
            }
        }

        stage('Inject Secure AppSettings') {
            steps {
                echo 'Configuring secure appsettings.json from Jenkins Credentials...'
                // IMPORTANT: In Jenkins, create a "Secret file" credential with the ID 'landingmvc-appsettings-prod'
                // This securely injects production database strings, API keys, etc., without committing them to git.
                withCredentials([file(credentialsId: 'landingmvc-appsettings-prod', variable: 'SECURE_APPSETTINGS')]) {
                    // Copy the secret file from Jenkins to overwrite the local appsettings.json before docker build
                    sh 'cp $SECURE_APPSETTINGS appsettings.json'
                }
            }
        }

        stage('Docker Build') {
            steps {
                echo 'Building the Docker container...'
                // Build the container using the Dockerfile and the injected secure appsettings
                sh "docker build -t ${DOCKER_REGISTRY}/${IMAGE_NAME}:${BUILD_NUMBER} ."
                sh "docker tag ${DOCKER_REGISTRY}/${IMAGE_NAME}:${BUILD_NUMBER} ${DOCKER_REGISTRY}/${IMAGE_NAME}:latest"
            }
        }

        stage('Docker Push') {
            steps {
                echo 'Pushing image to registry...'
                // Ensure Jenkins has credentials to push to the registry using withCredentials
                // withCredentials([usernamePassword(credentialsId: 'docker-registry-credentials', usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
                //     sh "echo $DOCKER_PASS | docker login ${DOCKER_REGISTRY} -u $DOCKER_USER --password-stdin"
                //     sh "docker push ${DOCKER_REGISTRY}/${IMAGE_NAME}:${BUILD_NUMBER}"
                //     sh "docker push ${DOCKER_REGISTRY}/${IMAGE_NAME}:latest"
                // }
                echo 'Skipping docker push - uncomment the block above when credentials are configured.'
            }
        }
    }

    post {
        always {
            // Clean up workspace after build to remove secrets and artifacts
            echo 'Cleaning up workspace...'
            cleanWs()
        }
        success {
            echo 'Build and Deployment Successful!'
        }
        failure {
            echo 'Pipeline failed. Check the logs for details.'
        }
    }
}
