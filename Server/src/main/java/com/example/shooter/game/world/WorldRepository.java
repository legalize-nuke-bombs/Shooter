package com.example.shooter.game.world;

import com.example.shooter.game.player.PlayerRole;
import jakarta.persistence.LockModeType;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import org.springframework.data.jpa.repository.Query;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface WorldRepository extends JpaRepository<World, UUID> {
    @Query("SELECT COUNT(w) FROM World w WHERE w.createdAt >= ?1")
    long countCreatedSince(Long timestamp);

    @Query("SELECT COUNT(w) FROM World w WHERE w.accessedAt >= ?1")
    long countAccessedSince(Long timestamp);

    @Query("""
SELECT w FROM World w
WHERE w.visibilityPolicy = ?1 AND w.joinPolicy = ?2
ORDER BY w.players DESC, w.accessedAt DESC, w.id
""")
    List<World> findByVisibilityPolicyAndJoinPolicyOrderedByPlayers(WorldVisibilityPolicy visibilityPolicy, WorldJoinPolicy joinPolicy, Pageable pageable);

    @Query("""
SELECT w FROM Player p JOIN p.world w
WHERE p.user.id = ?1 AND p.role >= ?2
ORDER BY p.lastSeen DESC, w.id
""")
    List<World> findByUserIdAndPlayerRoleOrderedByLastSeen(Long userId, PlayerRole playerRole, Pageable pageable);

    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("SELECT w FROM World w WHERE w.id = ?1")
    Optional<World> findByIdForPessimisticWrite(UUID worldId);

    @Query("SELECT w.id FROM World w")
    List<UUID> findAllIds();
}
