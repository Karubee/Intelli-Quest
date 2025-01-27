#!/bin/bash

# Set the region (e.g., us, eu, asia) and your project ID
REGION="us"
PROJECT_ID="intelliquest-3401f"
REPO_NAME="firstrepo"  # Name of the repository

# Optional: Set a retention period in days (e.g., 30 days) to delete images older than this
RETENTION_DAYS=0

# Fetch all image tags from GCR that are older than $RETENTION_DAYS or untagged
# Modify this query if you want to delete specific images, e.g., untagged only
gcloud container images list-tags gcr.io/$PROJECT_ID/$REPO_NAME \
    --limit=unlimited \
    --filter="timestamp.datetime < '-P${RETENTION_DAYS}D' OR tags:''" \
    --format='get(digest)' | while read -r DIGEST; do
        echo "Deleting image with digest: $DIGEST"
        # Delete the images based on digest
        gcloud container images delete gcr.io/$PROJECT_ID/$REPO_NAME@$DIGEST --force-delete-tags --quiet
done

echo "Image cleanup completed."
