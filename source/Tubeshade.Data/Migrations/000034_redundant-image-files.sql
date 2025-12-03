ALTER TABLE media.video_images
    DROP CONSTRAINT video_images_image_id_fkey,
    ADD CONSTRAINT video_images_image_id_fkey FOREIGN KEY (image_id) REFERENCES media.image_files (id) ON DELETE CASCADE;

WITH latest AS (SELECT videos.id,
                       MAX(image_files.created_at) AS created_at
                FROM media.image_files
                         INNER JOIN media.video_images ON video_images.image_id = image_files.id
                         INNER JOIN media.videos ON video_images.video_id = videos.id
                GROUP BY videos.id
                HAVING count(*) > 1)
DELETE
FROM media.image_files
WHERE id IN
      (SELECT video_images.image_id
       FROM latest
                INNER JOIN media.videos ON latest.id = videos.id
                INNER JOIN media.video_images ON videos.id = video_images.video_id
       WHERE video_images.image_id = image_files.id)
  AND created_at NOT IN
      (SELECT latest.created_at
       FROM latest
                INNER JOIN media.videos ON latest.id = videos.id
                INNER JOIN media.video_images ON videos.id = video_images.video_id
       WHERE video_images.image_id = image_files.id);
