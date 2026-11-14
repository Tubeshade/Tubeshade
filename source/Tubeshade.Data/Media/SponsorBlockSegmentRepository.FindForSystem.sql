SELECT id,
       public_id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       (SELECT videos.public_id FROM media.videos WHERE videos.id = sponsorblock_segments.video_id) AS video_id,
       external_id,
       start_time,
       end_time,
       category,
       action,
       locked,
       description
FROM media.sponsorblock_segments
WHERE sponsorblock_segments.public_id = @Id;
