ALTER TABLE media.channel_images
    DROP CONSTRAINT channel_images_image_id_fkey,
    ADD CONSTRAINT channel_images_image_id_fkey FOREIGN KEY (image_id) REFERENCES media.image_files (id) ON DELETE CASCADE;

-- Downloading images again overwrote the existing files with the same name, but added another image for each changed file,
-- so only the latest image of each file describes its current content.
DELETE
FROM media.image_files
WHERE id IN (SELECT image_id
             FROM (SELECT links.image_id,
                          row_number() OVER (PARTITION BY links.parent_id, image_files.storage_path
                              ORDER BY image_files.created_at DESC, image_files.id) AS row_number
                   FROM (SELECT video_id AS parent_id, image_id
                         FROM media.video_images
                         UNION ALL
                         SELECT channel_id, image_id
                         FROM media.channel_images
                         UNION ALL
                         SELECT playlist_id, image_id
                         FROM media.playlist_images) AS links
                            INNER JOIN media.image_files ON image_files.id = links.image_id) AS images
             WHERE row_number > 1);
