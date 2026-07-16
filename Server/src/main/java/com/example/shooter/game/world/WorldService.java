package com.example.shooter.game.world;

import com.example.shooter.exception.ApiException;
import com.example.shooter.exception.ErrorCode;
import com.example.shooter.game.player.Player;
import com.example.shooter.game.player.PlayerRepository;
import com.example.shooter.game.player.PlayerRepresentation;
import com.example.shooter.game.player.PlayerRole;
import com.example.shooter.jwt.UnityServerTokenProvider;
import com.example.shooter.user.User;
import com.example.shooter.user.UserRepository;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Instant;
import java.util.*;
import java.util.stream.Collectors;

@Service
@Slf4j
public class WorldService {

    private final UserRepository userRepository;
    private final WorldRepository worldRepository;
    private final PlayerRepository playerRepository;
    private final UnityServerTokenProvider unityServerTokenProvider;
    private final WorldSupportService worldSupportService;
    private final WorldUnityHookService worldUnityHookService;

    public WorldService(UserRepository userRepository, WorldRepository worldRepository, PlayerRepository playerRepository, UnityServerTokenProvider unityServerTokenProvider, WorldSupportService worldSupportService, WorldUnityHookService worldUnityHookService) {
        this.userRepository = userRepository;
        this.worldRepository = worldRepository;
        this.playerRepository = playerRepository;
        this.unityServerTokenProvider = unityServerTokenProvider;
        this.worldSupportService = worldSupportService;
        this.worldUnityHookService = worldUnityHookService;
    }

    @Transactional
    public WorldRepresentation create(Long userId, CreateWorldRequest request) {
        User user = userRepository.findById(userId).orElseThrow(() -> new ApiException(ErrorCode.NOT_AUTHENTICATED));

        Long now = Instant.now().getEpochSecond();

        World world = new World();
        world.setName(request.getName());
        world.setCreatedAt(now);
        world.setAccessedAt(now);
        world.setVisibilityPolicy(request.getVisibilityPolicy());
        world.setJoinPolicy(request.getJoinPolicy());
        world.setDescription(request.getDescription());
        world = worldRepository.save(world);

        Player player = new Player();
        player.setWorld(world);
        player.setUser(user);
        player.setRole(PlayerRole.CREATOR);
        player.setMemberSince(now);
        player = playerRepository.save(player);

        log.info("user {} created new world {} player {}", userId, world.getId(), player.getId());

        return new WorldRepresentation(
                world,
                List.of(new PlayerRepresentation(player))
        );
    }

    @Transactional
    public WorldJoinResponse join(Long userId, UUID worldId) {
        User user = userRepository.findById(userId).orElseThrow(() -> new ApiException(ErrorCode.NOT_AUTHENTICATED));
        Long now = Instant.now().getEpochSecond();

        World world = worldRepository.findByIdForPessimisticWrite(worldId).orElseThrow(() -> new ApiException(ErrorCode.WORLD_NOT_FOUND));
        world.setAccessedAt(now);
        worldRepository.save(world);

        Player player = playerRepository.findByUserIdAndWorldId(userId, worldId).orElse(null);

        if (player != null) {
            log.info("user {} came back to world {}", userId, worldId);
            return new WorldJoinResponse(
                    unityServerTokenProvider.generateToken(userId + ":" + worldId)
            );
        }

        if (world.getJoinPolicy() != WorldJoinPolicy.EVERYONE) {
            log.info("user {} couldn't join world {}: closen world join policy", userId, worldId);
            // smart shit
            if (world.getVisibilityPolicy() == WorldVisibilityPolicy.PUBLIC) {
                throw new ApiException(ErrorCode.WORLD_DOES_NOT_ACCEPT_NEW_MEMBERS);
            }
            throw new ApiException(ErrorCode.WORLD_NOT_FOUND);
        }

        player = new Player();
        player.setWorld(world);
        player.setUser(user);
        player.setMemberSince(now);
        player.setRole(PlayerRole.MEMBER);
        playerRepository.save(player);

        log.info("user {} joined world {} for the first time!", userId, worldId);
        return new WorldJoinResponse(
                unityServerTokenProvider.generateToken(userId + ":" + worldId)
        );
    }

    @Transactional
    public void leave(Long userId, UUID worldId) {
        if (playerRepository.deleteByUserIdAndWorldId(userId, worldId) > 0) {
            log.info("user {} left the world {}", userId, worldId);
        }
        else {
            log.info("user {} tried to leave the world {} where they are not a member", userId, worldId);
            throw new ApiException(ErrorCode.NOT_A_MEMBER);
        }
        worldSupportService.fix(worldId);
        worldUnityHookService.registerTask(new UnityHook(userId, worldId));
    }

    @Transactional
    public void kick(Long userId, UUID worldId, Long targetId) {
        if (Objects.equals(userId, targetId)) {
            log.info("user {} tried to kick themselves from the world {}", userId, worldId);
            throw new ApiException(ErrorCode.CANT_SELF_KICK);
        }

        Player player = playerRepository.findByUserIdAndWorldId(userId, worldId).orElseThrow(() -> new ApiException(ErrorCode.NOT_A_MEMBER));
        Player target = playerRepository.findByUserIdAndWorldId(targetId, worldId).orElseThrow(() -> new ApiException(ErrorCode.PLAYER_NOT_FOUND));

        if (player.getRole().ordinal() <= target.getRole().ordinal()) {
            log.info("user {} {} tried to kick target {} {} from the world {}", userId, player.getRole(), targetId, target.getRole(), worldId);
            throw new ApiException(ErrorCode.CANT_KICK_THIS_USER);
        }

        playerRepository.delete(target);
        log.info("user {} kicked target {} from the world {}", userId, targetId, worldId);
        worldSupportService.fix(worldId);
        worldUnityHookService.registerTask(new UnityHook(targetId, worldId));
    }

    @Transactional
    public WorldRepresentation patch(Long userId, UUID worldId, PatchWorldRequest request) {
        if (request.getName() == null && request.getDescription() == null
                && request.getVisibilityPolicy() == null && request.getJoinPolicy() == null) {
            throw new ApiException(ErrorCode.EMPTY_REQUEST);
        }

        World world = worldRepository.findByIdForPessimisticWrite(worldId).orElseThrow(() -> new ApiException(ErrorCode.WORLD_NOT_FOUND));
        requireCreator(userId, world);

        if (request.getName() != null) world.setName(request.getName());
        if (request.getDescription() != null) world.setDescription(request.getDescription());
        if (request.getVisibilityPolicy() != null) world.setVisibilityPolicy(request.getVisibilityPolicy());
        if (request.getJoinPolicy() != null) world.setJoinPolicy(request.getJoinPolicy());
        world = worldRepository.save(world);

        log.info("user {} patched world {}", userId, worldId);

        List<PlayerRepresentation> players = playerRepository.findAllByWorldIdsWithWorldsAndUsers(Set.of(worldId))
                .stream()
                .map(PlayerRepresentation::new)
                .toList();
        return new WorldRepresentation(world, players);
    }

    @Transactional
    public void delete(Long userId, UUID worldId) {
        World world = worldRepository.findByIdForPessimisticWrite(worldId).orElseThrow(() -> new ApiException(ErrorCode.WORLD_NOT_FOUND));
        requireCreator(userId, world);

        worldRepository.delete(world);
        log.info("user {} deleted world {}", userId, worldId);
        worldUnityHookService.registerTask(new UnityHook(null, worldId));
    }

    private void requireCreator(Long userId, World world) {
        Player player = playerRepository.findByUserIdAndWorldId(userId, world.getId()).orElse(null);
        if (player != null && player.getRole() == PlayerRole.CREATOR) return;

        if (player == null && world.getVisibilityPolicy() != WorldVisibilityPolicy.PUBLIC) {
            log.info("user {} tried to manage hidden world {} not being a member", userId, world.getId());
            throw new ApiException(ErrorCode.WORLD_NOT_FOUND);
        }

        log.info("user {} tried to manage world {} not being a creator", userId, world.getId());
        throw new ApiException(ErrorCode.NOT_A_CREATOR);
    }

    public List<WorldRepresentation> get(Long userId, PlayerRole playerRole, Integer page, Integer size) {
        Set<UUID> requiredWorldIds;
        WorldVisibilityPolicy requiredVisibilityPolicy;
        WorldJoinPolicy requiredJoinPolicy;
        if (playerRole == null) {
            requiredWorldIds = null;
            requiredVisibilityPolicy = WorldVisibilityPolicy.PUBLIC;
            requiredJoinPolicy = WorldJoinPolicy.EVERYONE;
        }
        else {
            requiredWorldIds = playerRepository.findAllByUserIdAndPlayerRoleWithWorlds(userId, playerRole)
                    .stream()
                    .map(Player::getWorld)
                    .map(World::getId)
                    .collect(Collectors.toSet());
            requiredVisibilityPolicy = null;
            requiredJoinPolicy = null;
        }

        Pageable pageable = PageRequest.of(page, size);

        List<World> worlds = worldRepository.findByWorldIdsAndVisibilityPolicyAccessedAtOrder(requiredWorldIds, requiredVisibilityPolicy, requiredJoinPolicy, pageable);
        Set<UUID> worldIds = worlds.stream().map(World::getId).collect(Collectors.toSet());

        List<Player> players = playerRepository.findAllByWorldIdsWithWorldsAndUsers(worldIds);

        Map<UUID, WorldRepresentation> resultMap = new LinkedHashMap<>();
        for (World w : worlds) {
            resultMap.put(w.getId(), new WorldRepresentation(w, new ArrayList<>()));
        }
        for (Player p : players) {
            resultMap.get(p.getWorld().getId()).getPlayers().add(new PlayerRepresentation(p));
        }

        return resultMap.values().stream().toList();
    }
}
